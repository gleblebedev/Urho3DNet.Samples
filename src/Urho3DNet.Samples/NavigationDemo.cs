using System.Collections.Generic;
using System.Linq;

namespace Urho3DNet.Samples
{
    public class NavigationDemo : Sample
    {
        /// Jack scene node.
        private readonly SharedPtr<Node> jackNode_ = new SharedPtr<Node>();

        /// Streaming distance.
        private readonly int streamingDistance_ = 2;

        /// Last calculated path.
        private readonly Vector3List currentPath_ = new Vector3List();

        /// Tile data.
        private readonly Dictionary<IntVector2, ByteVector> tileData_ = new Dictionary<IntVector2, ByteVector>();

        /// Added tiles.
        private readonly HashSet<IntVector2> addedTiles_ = new HashSet<IntVector2>();

        /// Path end position.
        private Vector3 endPos_;

        /// Flag for drawing debug geometry.
        private bool drawDebug_;

        /// Flag for using navigation mesh streaming.
        private bool useStreaming_;

        public NavigationDemo(Context context) : base(context)
        {
        }

        public Node JackNode
        {
            get => jackNode_;
            set => jackNode_.Value = value;
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Create the scene content
            CreateScene();

            // Create the UI content
            CreateUI();

            // Setup the viewport for displaying the scene
            SetupViewport();

            // Hook up to the frame update and render post-update events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmRelative);
        }

        private void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();

            Scene = new Scene(Context);

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Also create a DebugRenderer component so that we can draw debug geometry
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<DebugRenderer>();

            // Create scene node & StaticModel component for showing a static plane
            var planeNode = Scene.CreateChild("Plane");
            planeNode.SetScale(new Vector3(100.0f, 1.0f, 100.0f));
            var planeObject = planeNode.CreateComponent<StaticModel>();
            planeObject.SetModel(cache.GetResource<Model>("Models/Plane.mdl"));
            planeObject.SetMaterial(cache.GetResource<Material>("Materials/StoneTiled.xml"));

            // Create a Zone component for ambient lighting & fog control
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;

            // Create a directional light to the world. Enable cascaded shadows on it
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.6f, -1.0f, 0.8f);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            // Create some mushrooms
            const uint NUM_MUSHROOMS = 100;
            for (uint i = 0; i < NUM_MUSHROOMS; ++i)
                CreateMushroom(new Vector3(MathDefs.Random(90.0f) - 45.0f, 0.0f, MathDefs.Random(90.0f) - 45.0f));

            // Create randomly sized boxes. If boxes are big enough, make them occluders
            const uint NUM_BOXES = 20;
            for (uint i = 0; i < NUM_BOXES; ++i)
            {
                var boxNode = Scene.CreateChild("Box");
                var size = 1.0f + MathDefs.Random(10.0f);
                boxNode.Position = new Vector3(MathDefs.Random(80.0f) - 40.0f, size * 0.5f,
                    MathDefs.Random(80.0f) - 40.0f);
                boxNode.SetScale(size);
                var boxObject = boxNode.CreateComponent<StaticModel>();
                boxObject.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
                boxObject.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));
                boxObject.CastShadows = true;
                if (size >= 3.0f)
                    boxObject.IsOccluder = true;
            }

            // Create Jack node that will follow the path
            JackNode = Scene.CreateChild("Jack");
            JackNode.Position = new Vector3(-5.0f, 0.0f, 20.0f);
            var modelObject = JackNode.CreateComponent<AnimatedModel>();
            modelObject.SetModel(cache.GetResource<Model>("Models/Jack.mdl"));
            modelObject.SetMaterial(cache.GetResource<Material>("Materials/Jack.xml"));
            modelObject.CastShadows = true;

            // Create a NavigationMesh component to the scene root
            var navMesh = Scene.CreateComponent<NavigationMesh>();
            // Set small tiles to show navigation mesh streaming
            navMesh.TileSize = 32;
            // Create a Navigable component to the scene root. This tags all of the geometry in the scene as being part of the
            // navigation mesh. By default this is recursive, but the recursion could be turned off from Navigable
            Scene.CreateComponent<Navigable>();
            // Add padding to the navigation mesh in Y-direction so that we can add objects on top of the tallest boxes
            // in the scene and still update the mesh correctly
            navMesh.Padding = new Vector3(0.0f, 10.0f);
            // Now build the navigation geometry. This will take some time. Note that the navigation mesh will prefer to use
            // physics geometry from the scene nodes, as it often is simpler, but if it can not find any (like in this example)
            // it will use renderable geometry instead
            navMesh.Build();

            // Create the camera. Limit far clip distance to match the fog
            CameraNode = Scene.CreateChild("Camera");
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;

            // Set an initial position for the camera scene node above the plane and looking down
            CameraNode.Position = new Vector3(0.0f, 50.0f);
            pitch_ = 80.0f;
            CameraNode.Rotation = new Quaternion(pitch_, yaw_, 0.0f);
        }

        private void CreateUI()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();

            // Create a Cursor UI element because we want to be able to hide and show it at will. When hidden, the mouse cursor will
            // control the camera, and when visible, it will point the raycast target
            var style = cache.GetResource<XMLFile>("UI/DefaultStyle.xml");
            var cursor = new Cursor(Context);
            cursor.SetStyleAuto(style);
            ui.Cursor = cursor;

            // Set starting position of the cursor at the rendering window center
            var graphics = GetSubsystem<Graphics>();
            cursor.Position = new IntVector2(graphics.Width / 2, graphics.Height / 2);

            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Use WASD keys to move, RMB to rotate view\n" +
                "LMB to set destination, SHIFT+LMB to teleport\n" +
                "MMB or O key to add or remove obstacles\n" +
                "Tab to toggle navigation mesh streaming\n" +
                "Space to toggle debug geometry"
            );
            instructionText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            // The text has multiple rows. Center them in relation to each other
            instructionText.TextAlignment = HorizontalAlignment.HaCenter;

            // Position the text relative to the screen center
            instructionText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionText.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionText.Position = new IntVector2(0, ui.Root.Height / 4);
        }

        private void SetupViewport()
        {
            var renderer = GetSubsystem<Renderer>();

            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
            var viewport = new Viewport(Context, Scene, CameraNode.GetComponent<Camera>());
            renderer.SetViewport(0, viewport);
        }

        private void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);

            // Subscribe HandlePostRenderUpdate() function for processing the post-render update event, during which we request
            // debug geometry
            SubscribeToEvent(E.PostRenderUpdate, HandlePostRenderUpdate);
        }

        private void MoveCamera(float timeStep)
        {
            // Right mouse button controls mouse cursor visibility: hide when pressed
            var ui = GetSubsystem<UI>();
            var input = GetSubsystem<Input>();
            ui.Cursor.IsVisible = !input.GetMouseButtonDown(MouseButton.MousebRight);

            // Do not move if the UI has a focused element (the console)
            if (ui.GetFocusElement() != null)
                return;

            // Movement speed as world units per second
            const float MOVE_SPEED = 20.0f;
            // Mouse sensitivity as degrees per pixel
            const float MOUSE_SENSITIVITY = 0.1f;

            // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
            // Only move the camera when the cursor is hidden
            if (!ui.Cursor.IsVisible)
            {
                var mouseMove = input.MouseMove;
                yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
                pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
                pitch_ = MathDefs.Clamp(pitch_, -90.0f, 90.0f);

                // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
                CameraNode.Rotation = new Quaternion(pitch_, yaw_, 0.0f);
            }

            // Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
            if (input.GetKeyDown(Key.KeyW))
                CameraNode.Translate(Vector3.Forward * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyS))
                CameraNode.Translate(Vector3.Back * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyA))
                CameraNode.Translate(Vector3.Left * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyD))
                CameraNode.Translate(Vector3.Right * MOVE_SPEED * timeStep);

            // Set destination or teleport with left mouse button
            if (input.GetMouseButtonPress(MouseButton.MousebLeft))
                SetPathPoint();
            // Add or remove objects with middle mouse button, then rebuild navigation mesh partially
            if (input.GetMouseButtonPress(MouseButton.MousebMiddle) || input.GetKeyPress(Key.KeyO))
                AddOrRemoveObject();

            // Toggle debug geometry with space
            if (input.GetKeyPress(Key.KeySpace))
                drawDebug_ = !drawDebug_;
        }

        private void SetPathPoint()
        {
            Vector3 hitPos;
            Drawable hitDrawable;
            var navMesh = Scene.GetComponent<NavigationMesh>();

            if (Raycast(250.0f, out hitPos, out hitDrawable))
            {
                var pathPos = navMesh.FindNearestPoint(hitPos, new Vector3(1.0f, 1.0f, 1.0f));

                if (GetSubsystem<Input>().GetQualifierDown(Qualifier.QualShift))
                {
                    // Teleport
                    currentPath_.Clear();
                    JackNode.LookAt(new Vector3(pathPos.X, JackNode.Position.Y, pathPos.Z), Vector3.Up);
                    JackNode.Position = pathPos;
                }
                else
                {
                    // Calculate path from Jack's current position to the end point
                    endPos_ = pathPos;
                    navMesh.FindPath(currentPath_, JackNode.Position, endPos_);
                }
            }
        }

        private void AddOrRemoveObject()
        {
            // Raycast and check if we hit a mushroom node. If yes, remove it, if no, create a new one
            Vector3 hitPos;
            Drawable hitDrawable;

            if (!useStreaming_ && Raycast(250.0f, out hitPos, out hitDrawable))
            {
                // The part of the navigation mesh we must update, which is the world bounding box of the associated
                // drawable component
                BoundingBox updateBox;

                var hitNode = hitDrawable.Node;
                if (hitNode.Name == "Mushroom")
                {
                    updateBox = hitDrawable.WorldBoundingBox;
                    hitNode.Remove();
                }
                else
                {
                    var newNode = CreateMushroom(hitPos);
                    updateBox = newNode.GetComponent<StaticModel>().WorldBoundingBox;
                }

                // Rebuild part of the navigation mesh, then recalculate path if applicable
                var navMesh = Scene.GetComponent<NavigationMesh>();
                navMesh.Build(updateBox);
                if (currentPath_.Count > 0)
                    navMesh.FindPath(currentPath_, JackNode.Position, endPos_);
            }
        }

        private Node CreateMushroom(Vector3 pos)
        {
            var cache = GetSubsystem<ResourceCache>();

            var mushroomNode = Scene.CreateChild("Mushroom");
            mushroomNode.Position = pos;
            mushroomNode.Rotation = new Quaternion(0.0f, MathDefs.Random(360.0f), 0.0f);
            mushroomNode.SetScale(2.0f + MathDefs.Random(0.5f));
            var mushroomObject = mushroomNode.CreateComponent<StaticModel>();
            mushroomObject.SetModel(cache.GetResource<Model>("Models/Mushroom.mdl"));
            mushroomObject.SetMaterial(cache.GetResource<Material>("Materials/Mushroom.xml"));
            mushroomObject.CastShadows = true;

            return mushroomNode;
        }

        private bool Raycast(float maxDistance, out Vector3 hitPos, out Drawable hitDrawable)
        {
            hitDrawable = null;
            hitPos = Vector3.Zero;

            var ui = GetSubsystem<UI>();
            var pos = ui.CursorPosition;
            // Check the cursor is visible and there is no UI element in front of the cursor
            if (!ui.Cursor.IsVisible || ui.GetElementAt(pos, true) != null)
                return false;

            pos = ui.ConvertUIToSystem(pos);

            var graphics = GetSubsystem<Graphics>();
            var camera = CameraNode.GetComponent<Camera>();
            var cameraRay =
                camera.GetScreenRay((float) pos.X / graphics.Width, (float) pos.Y / graphics.Height);
            // Pick only geometry objects, not eg. zones or lights, only get the first (closest) hit
            var results = new RayQueryResultList();
            var query = new RayOctreeQuery(results, cameraRay, RayQueryLevel.RayTriangle, maxDistance,
                DrawableFlags.DrawableGeometry);
            Scene.GetComponent<Octree>().RaycastSingle(query);
            if (results.Count > 0)
            {
                var result = results[0];
                hitPos = result.Position;
                hitDrawable = result.Drawable;
                return true;
            }

            return false;
        }

        private void FollowPath(float timeStep)
        {
            if (currentPath_.Count > 0)
            {
                var nextWaypoint = currentPath_[0]; // NB: currentPath[0] is the next waypoint in order

                // Rotate Jack toward next waypoint to reach and move. Check for not overshooting the target
                var move = 5.0f * timeStep;
                var distance = (JackNode.Position - nextWaypoint).Length;
                if (move > distance)
                    move = distance;

                JackNode.LookAt(nextWaypoint, Vector3.Up);
                JackNode.Translate(Vector3.Forward * move);

                // Remove waypoint if reached it
                if (distance < 0.1f)
                    currentPath_.RemoveAt(0);
            }
        }

        private void ToggleStreaming(bool enabled)
        {
            var navMesh = Scene.GetComponent<NavigationMesh>();
            if (enabled)
            {
                var maxTiles = (2 * streamingDistance_ + 1) * (2 * streamingDistance_ + 1);
                var boundingBox = navMesh.BoundingBox;
                SaveNavigationData();
                navMesh.Allocate(boundingBox, (uint) maxTiles);
            }
            else
            {
                navMesh.Build();
            }
        }

        private void UpdateStreaming()
        {
            // Center the navigation mesh at the jack
            var navMesh = Scene.GetComponent<NavigationMesh>();
            var jackTile = navMesh.GetTileIndex(JackNode.WorldPosition);
            var numTiles = navMesh.NumTiles;
            var beginTile = IntVector2.Max(IntVector2.Zero, jackTile - IntVector2.One * streamingDistance_);
            var endTile = IntVector2.Min(jackTile + IntVector2.One * streamingDistance_, numTiles - IntVector2.One);

            // Remove tiles
            foreach (var tileIdx in addedTiles_.ToArray())
                if (beginTile.X <= tileIdx.X && tileIdx.X <= endTile.X && beginTile.Y <= tileIdx.Y &&
                    tileIdx.Y <= endTile.Y)
                {
                }
                else
                {
                    navMesh.RemoveTile(tileIdx);
                    addedTiles_.Remove(tileIdx);
                }

            // Add tiles
            for (var z = beginTile.Y; z <= endTile.Y; ++z)
            for (var x = beginTile.X; x <= endTile.X; ++x)
            {
                var tileIdx = new IntVector2(x, z);
                if (!navMesh.HasTile(tileIdx) && tileData_.ContainsKey(tileIdx))
                {
                    addedTiles_.Add(tileIdx);
                    navMesh.AddTile(tileData_[tileIdx]);
                }
            }
        }

        private void SaveNavigationData()
        {
            var navMesh = Scene.GetComponent<NavigationMesh>();
            tileData_.Clear();
            addedTiles_.Clear();
            var numTiles = navMesh.NumTiles;
            for (var z = 0; z < numTiles.Y; ++z)
            for (var x = 0; x <= numTiles.X; ++x)
            {
                var tileIdx = new IntVector2(x, z);
                tileData_[tileIdx] = navMesh.GetTileData(tileIdx);
            }
        }

        private void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            var timeStep = eventData[E.Update.TimeStep].Float;

            // Move the camera, scale movement with time step
            MoveCamera(timeStep);

            // Make Jack follow the Detour path
            FollowPath(timeStep);

            // Update streaming
            var input = GetSubsystem<Input>();
            if (input.GetKeyPress(Key.KeyTab))
            {
                useStreaming_ = !useStreaming_;
                ToggleStreaming(useStreaming_);
            }

            if (useStreaming_)
                UpdateStreaming();
        }

        private void HandlePostRenderUpdate(StringHash eventType, VariantMap eventData)
        {
            // If draw debug mode is enabled, draw navigation mesh debug geometry
            if (drawDebug_)
                Scene.GetComponent<NavigationMesh>().DrawDebugGeometry(true);

            if (currentPath_.Count > 0)
            {
                // Visualize the current calculated path
                var debug = Scene.GetComponent<DebugRenderer>();
                debug.AddBoundingBox(
                    new BoundingBox(endPos_ - new Vector3(0.1f, 0.1f, 0.1f), endPos_ + new Vector3(0.1f, 0.1f, 0.1f)),
                    new Color(1.0f, 1.0f, 1.0f));

                // Draw the path with a small upward bias so that it does not clip into the surfaces
                var bias = new Vector3(0.0f, 0.05f);
                debug.AddLine(JackNode.Position + bias, currentPath_[0] + bias, new Color(1.0f, 1.0f, 1.0f));

                if (currentPath_.Count > 1)
                    for (var i = 0; i < currentPath_.Count - 1; ++i)
                        debug.AddLine(currentPath_[i] + bias, currentPath_[i + 1] + bias, new Color(1.0f, 1.0f, 1.0f));
            }
        }
    }
}