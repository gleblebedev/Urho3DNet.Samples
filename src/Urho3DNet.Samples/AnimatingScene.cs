using System;
using System.Collections.Generic;
using System.Text;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    class AnimatingScene : Sample
    {
        public AnimatingScene(Context context) : base(context)
        {
            // Register an object factory for our custom Rotator component so that we can create them to scene nodes
            Context.RegisterFactory<Rotator>();
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

        public override void Stop()
        {
            UnsubscribeFromEvent(E.Update);
            base.Stop();
        }

        void CreateScene()
        {
            Scene = new Scene(Context);

            // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
            // (-1000, -1000, -1000) to (1000, 1000, 1000)
            Scene.CreateComponent<Octree>();

            // Create a Zone component into a child scene node. The Zone controls ambient lighting and fog settings. Like the Octree,
            // it also defines its volume with a bounding box, but can be rotated (so it does not need to be aligned to the world X, Y
            // and Z axes.) Drawable objects "pick up" the zone they belong to and use it when rendering; several zones can exist
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            // Set same volume as the Octree, set a close bluish fog and some ambient light
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.05f, 0.1f, 0.15f);
            zone.FogColor = new Color(0.1f, 0.2f, 0.3f);
            zone.FogStart = 10.0f;
            zone.FogEnd = 100.0f;

            // Create randomly positioned and oriented box StaticModels in the scene
            const uint NUM_OBJECTS = 2000;
            for (uint i = 0; i < NUM_OBJECTS; ++i)
            {
                var boxNode = Scene.CreateChild();
                boxNode.Position = new Vector3(MathDefs.Random(200.0f) - 100.0f, MathDefs.Random(200.0f) - 100.0f, MathDefs.Random(200.0f) - 100.0f);
                // Orient using random pitch, yaw and roll Euler angles
                boxNode.Rotation = new Quaternion(MathDefs.Random(360.0f), MathDefs.Random(360.0f), MathDefs.Random(360.0f));
                var boxObject = boxNode.CreateComponent<StaticModel>();
                boxObject.Model = Context.ResourceCache.GetResource<Model>("Models/Box.mdl");
                boxObject.SetMaterial(Context.ResourceCache.GetResource<Material>("Materials/Stone.xml"));

                // Add our custom Rotator component which will rotate the scene node each frame, when the scene sends its update event.
                // The Rotator component derives from the base class LogicComponent, which has convenience functionality to subscribe
                // to the various update events, and forward them to virtual functions that can be implemented by subclasses. This way
                // writing logic/update components in C++ becomes similar to scripting.
                // Now we simply set same rotation speed for all objects
                var rotator = boxNode.CreateComponent<Rotator>();
                rotator.RotationSpeed = new Vector3(10.0f, 20.0f, 30.0f);
            }

            // Create the camera. Let the starting position be at the world origin. As the fog limits maximum visible distance, we can
            // bring the far clip plane closer for more effective culling of distant objects
            CameraNode = Scene.CreateChild("Camera");
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 100.0f;

            // Create a point light to the camera scene node
            var light = CameraNode.CreateComponent<Light>();
            light.LightType = LightType.LightPoint;
            light.Range = 30.0f;
        }

        void CreateInstructions()
        {
            // Construct new Text object, set string to display and font to use
            var instructionText = Context.UI.Root.CreateChild<Text>();
            instructionText.SetText("Use WASD keys and mouse/touch to move");
            instructionText.SetFont(Context.ResourceCache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);

            // Position the text relative to the screen center
            instructionText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionText.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionText.Position = new IntVector2(0, Context.UI.Root.Height / 4);
        }

        void SetupViewport()
        {
            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
            var viewport = new Viewport(Context, Scene, CameraNode.GetComponent<Camera>());
            Context.Renderer.SetViewport(0, viewport);
        }

        void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);
        }

        void MoveCamera(float timeStep)
        {
            // Do not move if the UI has a focused element (the console)
            if (Context.UI.FocusElement != null)
                return;

            // Movement speed as world units per second
            const float MOVE_SPEED = 20.0f;
            // Mouse sensitivity as degrees per pixel
            const float MOUSE_SENSITIVITY = 0.1f;

            // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
            IntVector2 mouseMove = Context.Input.MouseMove;
            yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
            pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
            pitch_ = MathDefs.Clamp(pitch_, -90.0f, 90.0f);

            // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
            CameraNode.Rotation = new Quaternion(pitch_, yaw_, 0.0f);

            // Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
            if (Context.Input.GetKeyDown(Key.KeyW))
                CameraNode.Translate(Vector3.Forward * MOVE_SPEED * timeStep);
            if (Context.Input.GetKeyDown(Key.KeyS))
                CameraNode.Translate(Vector3.Back * MOVE_SPEED * timeStep);
            if (Context.Input.GetKeyDown(Key.KeyA))
                CameraNode.Translate(Vector3.Left * MOVE_SPEED * timeStep);
            if (Context.Input.GetKeyDown(Key.KeyD))
                CameraNode.Translate(Vector3.Right * MOVE_SPEED * timeStep);
        }

        void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            float timeStep = eventData["TimeStep"].Float;

            // Move the camera, scale movement with time step
            MoveCamera(timeStep);
        }
    }
}
