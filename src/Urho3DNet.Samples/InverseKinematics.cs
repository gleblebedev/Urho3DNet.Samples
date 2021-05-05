//
// Copyright (c) 2008-2020 the Urho3D project.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using System;

namespace Urho3DNet.Samples
{
    //[ObjectFactory]
    public class InverseKinematics : Sample
    {
        private Node floorNode_;
        private Node jackNode_;
        private AnimationController jackAnimCtrl_;
        private Node leftFoot_;
        private Node rightFoot_;
        private IKEffector leftEffector_;
        private IKEffector rightEffector_;
        private IKSolver solver_;
        private Node cameraRotateNode_;
        private float floorPitch_;
        private float floorRoll_;
        private bool drawDebug_;

        public InverseKinematics(Context context) : base(context)
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

            GetSubsystem<Input>().SetMouseVisible(true);
        }

        private void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();

            Scene = new Scene(Context);

            // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<DebugRenderer>();
            Scene.CreateComponent<PhysicsWorld>();

            // Create scene node & StaticModel component for showing a static plane
            floorNode_ = Scene.CreateChild("Plane");
            floorNode_.SetScale(new Vector3(50.0f, 1.0f, 50.0f));
            var planeObject = floorNode_.CreateComponent<StaticModel>();
            planeObject.SetModel(cache.GetResource<Model>("Models/Plane.mdl"));
            planeObject.SetMaterial(cache.GetResource<Material>("Materials/StoneTiled.xml"));

            // Set up collision, we need to raycast to determine foot height
            floorNode_.CreateComponent<RigidBody>();
            var col = floorNode_.CreateComponent<CollisionShape>();
            col.SetBox(new Vector3(1, 0, 1));

            // Create a directional light to the world.
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.6f, -1.0f, 0.8f); // The direction vector does not need to be normalized
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00005f, 0.5f);
            // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);

            // Load Jack model
            jackNode_ = Scene.CreateChild("Jack");
            jackNode_.Rotation = new Quaternion(0.0f, 270.0f, 0.0f);
            var jack = jackNode_.CreateComponent<AnimatedModel>();
            jack.SetModel(cache.GetResource<Model>("Models/Jack.mdl"));
            jack.SetMaterial(cache.GetResource<Material>("Materials/Jack.xml"));
            jack.CastShadows = true;

            // Create animation controller and play walk animation
            jackAnimCtrl_ = jackNode_.CreateComponent<AnimationController>();
            jackAnimCtrl_.PlayExclusive("Models/Jack_Walk.ani", 0, true, 0.0f);

            // We need to attach two inverse kinematic effectors to Jack's feet to
            // control the grounding.
            leftFoot_ = jackNode_.GetChild("Bip01_L_Foot", true);
            rightFoot_ = jackNode_.GetChild("Bip01_R_Foot", true);
            leftEffector_ = leftFoot_.CreateComponent<IKEffector>();
            rightEffector_ = rightFoot_.CreateComponent<IKEffector>();
            // Control 2 segments up to the hips
            leftEffector_.ChainLength = 2;
            rightEffector_.ChainLength = 2;

            // For the effectors to work, an IKSolver needs to be attached to one of
            // the parent nodes. Typically, you want to place the solver as close as
            // possible to the effectors for optimal performance. Since in this case
            // we're solving the legs only, we can place the solver at the spine.
            var spine = jackNode_.GetChild("Bip01_Spine", true);
            solver_ = spine.CreateComponent<IKSolver>();

            // Two-bone solver is more efficient and more stable than FABRIK (but only
            // works for two bones, obviously).
            solver_.SetAlgorithm(IKSolver.Algorithm.TwoBone);

            // Disable auto-solving, which means we need to call Solve() manually
            solver_.SetFeature(IKSolver.Feature.AutoSolve, false);

            // Only enable this so the debug draw shows us the pose before solving.
            // This should NOT be enabled for any other reason (it does nothing and is
            // a waste of performance).
            solver_.SetFeature(IKSolver.Feature.UpdateOriginalPose, true);

            // Create the camera.
            cameraRotateNode_ = Scene.CreateChild("CameraRotate");
            CameraNode = cameraRotateNode_.CreateChild("Camera");
            CameraNode.CreateComponent<Camera>();

            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = new Vector3(0, 0, -4);
            cameraRotateNode_.Position = new Vector3(0, 0.4f);
            pitch_ = 20;
            yaw_ = 50;
        }

        private void CreateInstructions()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();

            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Left-Click and drag to look around\nRight-Click and drag to change incline\nPress space to reset floor\nPress D to draw debug geometry");
            instructionText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);

            // Position the text relative to the screen center
            instructionText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionText.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionText.Position = new IntVector2(0, ui.Root.Height / 4);
        }

        private void SetupViewport()
        {
            var renderer = GetSubsystem<Renderer>();

            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen. We need to define the scene and the camera
            // at minimum. Additionally we could configure the viewport screen size and the rendering path (eg. forward / deferred) to
            // use, but now we just use full screen and default render path configured in the engine command line options
            Viewport = new Viewport(Context, Scene, CameraNode.GetComponent<Camera>());
            renderer.SetViewport(0, Viewport);
        }

        private void UpdateCameraAndFloor(float timeStep)
        {
            // Do not move if the UI has a focused element (the console)
            if (GetSubsystem<UI>().GetFocusElement() != null)
                return;

            var input = GetSubsystem<Input>();

            // Mouse sensitivity as degrees per pixel
            const float MOUSE_SENSITIVITY = 0.1f;

            // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
            if (input.GetMouseButtonDown(MouseButton.MousebLeft))
            {
                var mouseMove = input.MouseMove;
                yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
                pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
                pitch_ = MathDefs.Clamp(pitch_, -90.0f, 90.0f);
            }

            if (input.GetMouseButtonDown(MouseButton.MousebRight))
            {
                var mouseMoveInt = input.MouseMove;
                var mouseMove = new Matrix2(
                    -(float) Math.Cos(yaw_), (float) Math.Sin(yaw_),
                    (float) Math.Sin(yaw_), (float) Math.Cos(yaw_)
                ) * new Vector2(mouseMoveInt.Y, -mouseMoveInt.X);
                floorPitch_ += MOUSE_SENSITIVITY * mouseMove.X;
                floorPitch_ = MathDefs.Clamp(floorPitch_, -90.0f, 90.0f);
                floorRoll_ += MOUSE_SENSITIVITY * mouseMove.Y;
            }

            if (input.GetKeyPress(Key.KeySpace))
            {
                floorPitch_ = 0;
                floorRoll_ = 0;
            }

            if (input.GetKeyPress(Key.KeyD)) drawDebug_ = !drawDebug_;

            // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
            cameraRotateNode_.Rotation = new Quaternion(pitch_, yaw_, 0.0f);
            floorNode_.Rotation = new Quaternion(floorPitch_, 0, floorRoll_);
        }

        private void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);
            SubscribeToEvent(E.PostRenderUpdate, HandlePostRenderUpdate);
            SubscribeToEvent(E.SceneDrawableUpdateFinished, HandleSceneDrawableUpdateFinished);
        }

        private void HandleUpdate(VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            var timeStep = eventData[E.Update.TimeStep].Float;

            // Move the camera, scale movement with time step
            UpdateCameraAndFloor(timeStep);
        }

        private void HandlePostRenderUpdate(VariantMap eventData)
        {
            if (drawDebug_)
                solver_.DrawDebugGeometry(false);
        }

        private void HandleSceneDrawableUpdateFinished(VariantMap eventData)
        {
            var phyWorld = Scene.GetComponent<PhysicsWorld>();
            var leftFootPosition = leftFoot_.WorldPosition;
            var rightFootPosition = rightFoot_.WorldPosition;

            // Cast ray down to get the normal of the underlying surface
            var result = new PhysicsRaycastResult();
            phyWorld.RaycastSingle(result, new Ray(leftFootPosition + new Vector3(0, 1), new Vector3(0, -1)), 2);
            if (result.Body != null)
            {
                // Cast again, but this time along the normal. Set the target position
                // to the ray intersection
                phyWorld.RaycastSingle(result, new Ray(leftFootPosition + result.Normal, -result.Normal), 2);
                // The foot node has an offset relative to the root node
                var footOffset = leftFoot_.WorldPosition.Y - jackNode_.WorldPosition.Y;
                leftEffector_.TargetPosition = result.Position + result.Normal * footOffset;
                // Rotate foot according to normal
                leftFoot_.Rotate(new Quaternion(new Vector3(0, 1), result.Normal), TransformSpace.TsWorld);
            }

            // Same deal with the right foot
            phyWorld.RaycastSingle(result, new Ray(rightFootPosition + new Vector3(0, 1), new Vector3(0, -1)), 2);
            if (result.Body != null)
            {
                phyWorld.RaycastSingle(result, new Ray(rightFootPosition + result.Normal, -result.Normal), 2);
                var footOffset = rightFoot_.WorldPosition.Y - jackNode_.WorldPosition.Y;
                rightEffector_.TargetPosition = result.Position + result.Normal * footOffset;
                rightFoot_.Rotate(new Quaternion(new Vector3(0, 1), result.Normal), TransformSpace.TsWorld);
            }

            solver_.Solve();
        }
    }
}