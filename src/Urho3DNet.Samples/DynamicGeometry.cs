using System;
using System.Collections.Generic;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class DynamicGeometry : Sample
    {
        /// Cloned models' vertex buffers that we will animate.
        private readonly VertexBufferRefList animatingBuffers_ = new VertexBufferRefList();

        /// Original vertex positions for the sphere model.
        private readonly List<Vector3> originalVertices_ = new List<Vector3>();

        /// If the vertices are duplicates, indices to the original vertices (to allow seamless animation.)
        private readonly List<uint> vertexDuplicates_ = new List<uint>();

        private float time_;

        private bool animate_ = true;

        public DynamicGeometry(Context context) : base(context)
        {
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

            // Hook up to the frame update events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmRelative);
        }

        private unsafe void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();

            Scene = new Scene(Context);

            // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
            // (-1000, -1000, -1000) to (1000, 1000, 1000)
            Scene.CreateComponent<Octree>();

            // Create a Zone for ambient light & fog control
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.FogColor = new Color(0.2f, 0.2f, 0.2f);
            zone.FogStart = 200.0f;
            zone.FogEnd = 300.0f;

            // Create a directional light
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction =
                new Vector3(-0.6f, -1.0f, -0.8f); // The direction vector does not need to be normalized
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.Color = new Color(0.4f, 1.0f, 0.4f);
            light.SpecularIntensity = 1.5f;

            // Get the original model and its unmodified vertices, which are used as source data for the animation
            var originalModel = cache.GetResource<Model>("Models/Box.mdl");
            if (originalModel == null)
            {
                Log.Error("Model not found, cannot initialize example scene");
                return;
            }

            originalVertices_.Clear();
            vertexDuplicates_.Clear();

            // Get the vertex buffer from the first geometry's first LOD level
            var buffer = originalModel.GetGeometry(0, 0).GetVertexBuffer(0);
            var vertexDataPtr = (byte*) buffer.Lock(0, buffer.VertexCount);
            if (vertexDataPtr != null)
            {
                var numVertices = buffer.VertexCount;
                var vertexSize = buffer.GetVertexSize();
                // Copy the original vertex positions
                for (uint i = 0; i < numVertices; ++i)
                {
                    var src = (Vector3*) (vertexDataPtr + i * vertexSize);
                    originalVertices_.Add(*src);
                    vertexDuplicates_.Add(i);
                }

                buffer.Unlock();

                // Detect duplicate vertices to allow seamless animation
                for (var i = 0; i < originalVertices_.Count; ++i)
                {
                    vertexDuplicates_[i] = (uint) i; // Assume not a duplicate
                    for (var j = 0; j < i; ++j)
                        if (originalVertices_[i].Equals(originalVertices_[j]))
                        {
                            vertexDuplicates_[i] = (uint) j;
                            break;
                        }
                }
            }
            else
            {
                Log.Error("Failed to lock the model vertex buffer to get original vertices");
                return;
            }

            // Create StaticModels in the scene. Clone the model for each so that we can modify the vertex data individually
            for (var y = -1; y <= 1; ++y)
            for (var x = -1; x <= 1; ++x)
            {
                var node = Scene.CreateChild("Object");
                node.Position = new Vector3(x * 2.0f, 0.0f, y * 2.0f);
                var @object = node.CreateComponent<StaticModel>();
                var cloneModel = originalModel.Clone();
                @object.SetModel(cloneModel);
                // Store the cloned vertex buffer that we will modify when animating
                animatingBuffers_.Add(cloneModel.GetGeometry(0, 0).GetVertexBuffer(0));
            }

            // Finally create one model (pyramid shape) and a StaticModel to display it from scratch
            // Note: there are duplicated vertices to enable face normals. We will calculate normals programmatically
            {
                const uint numVertices = 18;

                float[] vertexData =
                {
                    // Position             Normal
                    0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f,
                    0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,
                    0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,

                    0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,
                    0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,

                    0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,

                    0.0f, 0.5f, 0.0f, 0.0f, 0.0f, 0.0f,
                    0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,

                    0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
                    0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,

                    0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, 0.5f, 0.0f, 0.0f, 0.0f,
                    -0.5f, -0.5f, -0.5f, 0.0f, 0.0f, 0.0f
                };

                ushort[] indexData =
                {
                    0, 1, 2,
                    3, 4, 5,
                    6, 7, 8,
                    9, 10, 11,
                    12, 13, 14,
                    15, 16, 17
                };

                // Calculate face normals now
                for (uint i = 0; i < numVertices; i += 3)
                {
                    var v1 = GetVector3At(vertexData, 6 * i);
                    var v2 = GetVector3At(vertexData, 6 * (i + 1));
                    var v3 = GetVector3At(vertexData, 6 * (i + 2));
                    var n1 = GetVector3At(vertexData, 6 * i + 3);
                    var n2 = GetVector3At(vertexData, 6 * (i + 1) + 3);
                    var n3 = GetVector3At(vertexData, 6 * (i + 2) + 3);

                    var edge1 = v1 - v2;
                    var edge2 = v1 - v3;
                    n1 = n2 = n3 = edge1.CrossProduct(edge2).Normalized;
                }

                var fromScratchModel = new Model(Context);
                var vb = new VertexBuffer(Context);
                var ib = new IndexBuffer(Context);
                var geom = new Geometry(Context);

                // Shadowed buffer needed for raycasts to work, and so that data can be automatically restored on device loss
                vb.IsShadowed = true;
                // We could use the "legacy" element bitmask to define elements for more compact code, but let's demonstrate
                // defining the vertex elements explicitly to allow any element types and order
                var elements = new VertexElementList();
                elements.Add(new VertexElement(VertexElementType.TypeVector3, VertexElementSemantic.SemPosition));
                elements.Add(new VertexElement(VertexElementType.TypeVector3, VertexElementSemantic.SemNormal));
                vb.SetSize(numVertices, elements);
                fixed (float* vertexPtr = vertexData)
                {
                    vb.SetData((IntPtr) vertexPtr);
                }

                ib.IsShadowed = true;
                ib.SetSize(numVertices, false);
                fixed (ushort* indexPtr = indexData)
                {
                    ib.SetData((IntPtr) indexPtr);
                }

                geom.SetVertexBuffer(0, vb);
                geom.IndexBuffer = ib;
                geom.SetDrawRange(PrimitiveType.TriangleList, 0, numVertices);

                fromScratchModel.NumGeometries = 1;
                fromScratchModel.SetGeometry(0, 0, geom);
                fromScratchModel.BoundingBox =
                    new BoundingBox(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f));

                // Though not necessary to render, the vertex & index buffers must be listed in the model so that it can be saved properly
                var vertexBuffers = new VertexBufferRefList();
                var indexBuffers = new IndexBufferRefList();
                vertexBuffers.Add(vb);
                indexBuffers.Add(ib);
                // Morph ranges could also be not defined. Here we simply define a zero range (no morphing) for the vertex buffer
                var morphRangeStarts = new UIntArray();
                var morphRangeCounts = new UIntArray();
                morphRangeStarts.Add(0);
                morphRangeCounts.Add(0);
                fromScratchModel.SetVertexBuffers(vertexBuffers, morphRangeStarts, morphRangeCounts);
                fromScratchModel.IndexBuffers = indexBuffers;

                var node = Scene.CreateChild("FromScratchObject");
                node.Position = new Vector3(0.0f, 3.0f);
                var @object = node.CreateComponent<StaticModel>();
                @object.SetModel(fromScratchModel);
            }

            // Create the camera
            CameraNode = new Node(Context);
            CameraNode.Position = new Vector3(0.0f, 2.0f, -20.0f);
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
        }

        private Vector3 GetVector3At(float[] vertexData, uint pos)
        {
            return new Vector3(vertexData[pos], vertexData[pos + 1], vertexData[pos + 2]);
        }

        private void CreateInstructions()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();

            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Use WASD keys and mouse/touch to move\n" +
                "Space to toggle animation"
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
        }

        private void MoveCamera(float timeStep)
        {
            // Do not move if the UI has a focused element (the console)
            if (Context.UI.GetFocusElement() != null)
                return;

            var input = GetSubsystem<Input>();

            // Movement speed as world units per second
            const float MOVE_SPEED = 20.0f;
            // Mouse sensitivity as degrees per pixel
            const float MOUSE_SENSITIVITY = 0.1f;

            // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
            var mouseMove = input.MouseMove;
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
        }

        private unsafe void AnimateObjects(float timeStep)
        {
            time_ += timeStep * 100.0f;

            // Repeat for each of the cloned vertex buffers
            for (var i = 0; i < animatingBuffers_.Count; ++i)
            {
                var startPhase = time_ + i * 30.0f;
                var buffer = animatingBuffers_[i];

                // Lock the vertex buffer for update and rewrite positions with sine wave modulated ones
                // Cannot use discard lock as there is other data (normals, UVs) that we are not overwriting
                var vertexData = (byte*) buffer.Lock(0, buffer.VertexCount);
                if (vertexData != null)
                {
                    var vertexSize = buffer.GetVertexSize();
                    var numVertices = buffer.VertexCount;
                    for (var j = 0; j < numVertices; ++j)
                    {
                        // If there are duplicate vertices, animate them in phase of the original
                        var phase = startPhase + vertexDuplicates_[j] * 10.0f;
                        var src = originalVertices_[j];
                        var dest = (Vector3*) (vertexData + j * vertexSize);
                        *dest = new Vector3(
                            src.X * (1.0f + 0.1f * (float) Math.Sin(MathDefs.DegreesToRadians(phase))),
                            src.Y * (1.0f + 0.1f * (float) Math.Sin(MathDefs.DegreesToRadians(phase + 60.0f))),
                            src.Z * (1.0f + 0.1f * (float) Math.Sin(MathDefs.DegreesToRadians(phase + 120.0f))));
                    }

                    buffer.Unlock();
                }
            }
        }

        private void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            var timeStep = eventData[E.Update.TimeStep].Float;

            // Toggle animation with space
            var input = Context.Input;
            if (input.GetKeyPress(Key.KeySpace))
                animate_ = !animate_;

            // Move the camera, scale movement with time step
            MoveCamera(timeStep);

            // Animate objects' vertex data if enabled
            if (animate_)
                AnimateObjects(timeStep);
        }
    }
}