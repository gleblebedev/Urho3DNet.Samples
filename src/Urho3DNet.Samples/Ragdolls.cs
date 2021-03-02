namespace Urho3DNet.Samples
{
    [ObjectFactory]
    public class Ragdolls : Sample
    {
        /// Flag for drawing debug geometry.
        bool drawDebug_;

        public Ragdolls(Context context) : base(context)
        {
            Context.RegisterFactory<CreateRagdoll>();
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Create the scene content
            CreateScene();

            // Create the UI content
            CreateInstructions();

            // Setup the viewport for displaying the scene
            SetupViewport();

            // Hook up to the frame update and render post-update events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmRelative);
        }

        void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();

            Scene = new Scene(Context);

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            // Create a physics simulation world with default parameters, which will update at 60fps. Like the Octree must
            // exist before creating drawable components, the PhysicsWorld must exist before creating physics components.
            // Finally, create a DebugRenderer component so that we can draw physics debug geometry
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<PhysicsWorld>();
            Scene.CreateComponent<DebugRenderer>();

            // Create a Zone component for ambient lighting & fog control
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = (100.0f);
            zone.FogEnd = (300.0f);

            // Create a directional light to the world. Enable cascaded shadows on it
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.6f, -1.0f, 0.8f);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = (true);
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            {
                // Create a floor object, 500 x 500 world units. Adjust position so that the ground is at zero Y
                var floorNode = Scene.CreateChild("Floor");
                floorNode.Position = new Vector3(0.0f, -0.5f, 0.0f);
                floorNode.SetScale(new Vector3(500.0f, 1.0f, 500.0f));
                var floorObject = floorNode.CreateComponent<StaticModel>();
                floorObject.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
                floorObject.SetMaterial(cache.GetResource<Material>("Materials/StoneTiled.xml"));

                // Make the floor physical by adding RigidBody and CollisionShape components
                var body = floorNode.CreateComponent<RigidBody>();
                // We will be spawning spherical objects in this sample. The ground also needs non-zero rolling friction so that
                // the spheres will eventually come to rest
                body.RollingFriction = 0.15f;
                var shape = floorNode.CreateComponent<CollisionShape>();
                // Set a box shape of size 1 x 1 x 1 for collision. The shape will be scaled with the scene node scale, so the
                // rendering and physics representation sizes should match (the box model is also 1 x 1 x 1.)
                shape.SetBox(Vector3.One);
            }

            // Create animated models
            for (int z = -1; z <= 1; ++z)
            {
                for (int x = -4; x <= 4; ++x)
                {
                    var modelNode = Scene.CreateChild("Jack");
                    modelNode.Position = new Vector3(x * 5.0f, 0.0f, z * 5.0f);
                    modelNode.Rotation = new Quaternion(0.0f, 180.0f, 0.0f);
                    var modelObject = modelNode.CreateComponent<AnimatedModel>();
                    modelObject.SetModel(cache.GetResource<Model>("Models/Jack.mdl"));
                    modelObject.SetMaterial(cache.GetResource<Material>("Materials/Jack.xml"));
                    modelObject.CastShadows = true;
                    // Set the model to also update when invisible to avoid staying invisible when the model should come into
                    // view, but does not as the bounding box is not updated
                    modelObject.UpdateInvisible = true;

                    // Create a rigid body and a collision shape. These will act as a trigger for transforming the
                    // model into a ragdoll when hit by a moving object
                    var body = modelNode.CreateComponent<RigidBody>();
                    // The Trigger mode makes the rigid body only detect collisions, but impart no forces on the
                    // colliding objects
                    body.IsTrigger = true;
                    var shape = modelNode.CreateComponent<CollisionShape>();
                    // Create the capsule shape with an offset so that it is correctly aligned with the model, which
                    // has its origin at the feet
                    shape.SetCapsule(0.7f, 2.0f, new Vector3(0.0f, 1.0f, 0.0f));

                    // Create a custom component that reacts to collisions and creates the ragdoll
                    modelNode.CreateComponent<CreateRagdoll>();
                }
            }

            // Create the camera. Limit far clip distance to match the fog. Note: now we actually create the camera node outside
            // the scene, because we want it to be unaffected by scene load / save
            CameraNode = new Node(Context);
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;

            // Set an initial position for the camera scene node above the floor
            CameraNode.Position = new Vector3(0.0f, 3.0f, -20.0f);
        }

        void CreateInstructions()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();

            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Use WASD keys and mouse/touch to move\n"+
                "LMB to spawn physics objects\n"+
                "F5 to save scene, F7 to load\n"+
                "Space to toggle physics debug geometry"
            );
            instructionText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            // The text has multiple rows. Center them in relation to each other
            instructionText.TextAlignment = HorizontalAlignment.HaCenter;

            // Position the text relative to the screen center
            instructionText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionText.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionText.Position = new  IntVector2(0, ui.Root.Height / 4);
        }

        void SetupViewport()
        {
            var renderer = GetSubsystem<Renderer>();

            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
            var viewport = new Viewport(Context, Scene, CameraNode.GetComponent<Camera>());
            renderer.SetViewport(0, viewport);
        }

        void MoveCamera(float timeStep)
        {
            // Do not move if the UI has a focused element (the console)
            if (GetSubsystem<UI>().GetFocusElement() != null)
                return;

            var input = GetSubsystem<Input>();

            // Movement speed as world units per second
            const float MOVE_SPEED = 20.0f;
            // Mouse sensitivity as degrees per pixel
            const float MOUSE_SENSITIVITY = 0.1f;

            // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
            IntVector2 mouseMove = input.MouseMove;
            yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
            pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
            pitch_ = MathDefs.Clamp(pitch_, -90.0f, 90.0f);

            // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
            CameraNode.Rotation = new Quaternion(pitch_, yaw_, 0.0f);

            // Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
            if (input.GetKeyDown(Key.KeyW))
                CameraNode.Translate(Vector3.Forward * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyS))
                CameraNode.Translate(Vector3.Back * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyA))
                CameraNode.Translate(Vector3.Left * MOVE_SPEED * timeStep);
            if (input.GetKeyDown(Key.KeyD))
                CameraNode.Translate(Vector3.Right * MOVE_SPEED * timeStep);

            // "Shoot" a physics object with left mousebutton
            if (input.GetMouseButtonPress(MouseButton.MousebLeft))
                SpawnObject();

            //    // Check for loading / saving the scene
            //    if (input.GetKeyPress(KEY_F5))
            //    {
            //        File saveFile(context_, GetSubsystem<FileSystem>().GetProgramDir() + "Data/Scenes/Ragdolls.xml", FILE_WRITE);
            //        Scene.SaveXML(saveFile);
            //    }
            //    if (input.GetKeyPress(KEY_F7))
            //    {
            //        File loadFile(context_, GetSubsystem<FileSystem>().GetProgramDir() + "Data/Scenes/Ragdolls.xml", FILE_READ);
            //        Scene.LoadXML(loadFile);
            //    }

            // Toggle physics debug geometry with space
            if (input.GetKeyPress(Key.KeySpace))
                drawDebug_ = !drawDebug_;
        }

        void SpawnObject()
        {
            var cache = GetSubsystem<ResourceCache>();

            var boxNode = Scene.CreateChild("Sphere");
            boxNode.Position = CameraNode.Position;
            boxNode.Rotation = CameraNode.Rotation;
            boxNode.SetScale(0.25f);
            var boxObject = boxNode.CreateComponent<StaticModel>();
            boxObject.SetModel(cache.GetResource<Model>("Models/Sphere.mdl"));
            boxObject.SetMaterial(cache.GetResource<Material>("Materials/StoneSmall.xml"));
            boxObject.CastShadows = true;

            var body = boxNode.CreateComponent<RigidBody>();
            body.Mass = (1.0f);
            body.RollingFriction = (0.15f);
            var shape = boxNode.CreateComponent<CollisionShape>();
            shape.SetSphere(1.0f);

            const float OBJECT_VELOCITY = 10.0f;

            // Set initial velocity for the RigidBody based on camera forward vector. Add also a slight up component
            // to overcome gravity better
            body.LinearVelocity = (CameraNode.Rotation * new Vector3(0.0f, 0.25f, 1.0f) * OBJECT_VELOCITY);
        }

        void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);

            // Subscribe HandlePostRenderUpdate() function for processing the post-render update event, during which we request
            // debug geometry
            SubscribeToEvent(E.PostRenderUpdate, HandlePostRenderUpdate);
        }

        void HandleUpdate(VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            float timeStep = eventData[E.Update.TimeStep].Float;

            // Move the camera, scale movement with time step
            MoveCamera(timeStep);
    }

        void HandlePostRenderUpdate(VariantMap eventData)
        {
            // If draw debug mode is enabled, draw physics debug geometry. Use depth test to make the result easier to interpret
            if (drawDebug_)
                Scene.GetComponent<PhysicsWorld>().DrawDebugGeometry(true);
        }

    }
}