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

namespace Urho3DNet.Samples
{
    [ObjectFactory]
    public class RaycastVehicleDemo : Sample
    {
        public const float YAW_SENSITIVITY = 0.1f;

        private const float CAMERA_DISTANCE = 10.0f;
        private readonly SharedPtr<Vehicle2> _vehicle = new SharedPtr<Vehicle2>(null);

        public RaycastVehicleDemo(Context context) : base(context)
        {
            // Register factory and attributes for the Vehicle component so it can be created via CreateComponent, and loaded / saved
            context.RegisterFactory<Vehicle2>();
        }

        private Vehicle2 Vehicle
        {
            get => _vehicle;
            set => _vehicle.Value = value;
        }


        public override void Start()
        {
            // Execute base class startup
            base.Start();
            // Create static scene content
            CreateScene();
            // Create the controllable vehicle
            CreateVehicle();
            // Create the UI content
            CreateInstructions();
            // Subscribe to necessary events
            SubscribeToEvents();
            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmRelative);
        }

        private void CreateScene()
        {
            var cache = GetSubsystem<ResourceCache>();
            Scene = new Scene(Context);
            // Create scene subsystem components
            Scene.CreateComponent<Octree>();
            Scene.CreateComponent<PhysicsWorld>();
            // Create camera and define viewport. We will be doing load / save, so it's convenient to create the camera outside the scene,
            // so that it won't be destroyed and recreated, and we don't have to redefine the viewport on load
            CameraNode = new Node(Context);
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 500.0f;
            GetSubsystem<Renderer>().SetViewport(0, new Viewport(Context, Scene, camera));
            // Create static scene content. First create a zone for ambient lighting and fog control
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 300.0f;
            zone.FogEnd = 500.0f;
            zone.SetBoundingBox(new BoundingBox(-2000.0f, 2000.0f));
            // Create a directional light with cascaded shadow mapping
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.3f, -0.5f, 0.425f);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);
            light.SpecularIntensity = 0.5f;
            // Create heightmap terrain with collision
            var terrainNode = Scene.CreateChild("Terrain");
            terrainNode.Position = Vector3.Zero;
            ;
            var terrain = terrainNode.CreateComponent<Terrain>();
            terrain.PatchSize = 64;
            terrain.Spacing =
                new Vector3(3.0f, 0.1f, 3.0f); // Spacing between vertices and vertical resolution of the height map
            terrain.Smoothing = true;
            terrain.HeightMap = cache.GetResource<Image>("Textures/HeightMap.png");
            terrain.Material = cache.GetResource<Material>("Materials/Terrain.xml");
            // The terrain consists of large triangles, which fits well for occlusion rendering, as a hill can occlude all
            // terrain patches and other objects behind it
            terrain.IsOccluder = true;
            var body = terrainNode.CreateComponent<RigidBody>();
            body.CollisionLayer = 2; // Use layer bitmask 2 for static geometry
            var shape = terrainNode.CreateComponent<CollisionShape>();
            shape.SetTerrain();
            // Create 1000 mushrooms in the terrain. Always face outward along the terrain normal
            const uint NUM_MUSHROOMS = 1000;
            for (uint i = 0; i < NUM_MUSHROOMS; ++i)
            {
                var objectNode = Scene.CreateChild("Mushroom");
                var position = new Vector3(MathDefs.Random(2000.0f) - 1000.0f, 0.0f,
                    MathDefs.Random(2000.0f) - 1000.0f);
                position.Y = terrain.GetHeight(position) - 0.1f;
                objectNode.Position = position;
                // Create a rotation quaternion from up vector to terrain normal
                objectNode.Rotation = new Quaternion(Vector3.Up, terrain.GetNormal(position));
                objectNode.SetScale(3.0f);
                var @object = objectNode.CreateComponent<StaticModel>();
                @object.SetModel(cache.GetResource<Model>("Models/Mushroom.mdl"));
                @object.SetMaterial(cache.GetResource<Material>("Materials/Mushroom.xml"));
                @object.CastShadows = true;
                body = objectNode.CreateComponent<RigidBody>();
                body.CollisionLayer = 2;
                shape = objectNode.CreateComponent<CollisionShape>();
                shape.SetTriangleMesh(@object.GetModel(), 0);
            }
        }

        private void CreateVehicle()
        {
            var vehicleNode = Scene.CreateChild("Vehicle");
            vehicleNode.Position = new Vector3(0.0f, 25.0f);
            // Create the vehicle logic component
            Vehicle = vehicleNode.CreateComponent<Vehicle2>();
            // Create the rendering and physics components
            Vehicle.Init();
        }

        private void CreateInstructions()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();
            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Use WASD keys to drive, F to brake, mouse/touch to rotate camera\n" +
                "F5 to save scene, F7 to load");
            instructionText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            // The text has multiple rows. Center them in relation to each other
            instructionText.TextAlignment = HorizontalAlignment.HaCenter;
            // Position the text relative to the screen center
            instructionText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionText.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionText.Position = new IntVector2(0, ui.Root.Height / 4);
        }

        private void SubscribeToEvents()
        {
            // Subscribe to Update event for setting the vehicle controls before physics simulation
            SubscribeToEvent(E.Update, HandleUpdate);
            // Subscribe to PostUpdate event for updating the camera position after physics simulation
            SubscribeToEvent(E.PostUpdate, HandlePostUpdate);
            // Unsubscribe the SceneUpdate event from base class as the camera node is being controlled in HandlePostUpdate() in this sample
            UnsubscribeFromEvent(E.SceneUpdate);
        }

        private void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            var input = GetSubsystem<Input>();
            if (Vehicle != null)
            {
                var ui = GetSubsystem<UI>();
                // Get movement controls and assign them to the vehicle component. If UI has a focused element, clear controls
                if (ui.GetFocusElement() == null)
                {
                    Vehicle.controls_.Set(Vehicle2.CTRL_FORWARD, input.GetKeyDown(Key.KeyW));
                    Vehicle.controls_.Set(Vehicle2.CTRL_BACK, input.GetKeyDown(Key.KeyS));
                    Vehicle.controls_.Set(Vehicle2.CTRL_LEFT, input.GetKeyDown(Key.KeyA));
                    Vehicle.controls_.Set(Vehicle2.CTRL_RIGHT, input.GetKeyDown(Key.KeyD));
                    Vehicle.controls_.Set(Vehicle2.CTRL_BRAKE, input.GetKeyDown(Key.KeyF));
                    // Add yaw & pitch from the mouse motion or touch input. Used only for the camera, does not affect motion
                    if (touchEnabled_)
                    {
                        for (uint i = 0; i < input.NumTouches; ++i)
                        {
                            var state = input.GetTouch(i);
                            if (state.TouchedElement == null) // Touch on empty space
                            {
                                var camera = CameraNode.GetComponent<Camera>();
                                if (camera == null) return;
                                var graphics = GetSubsystem<Graphics>();
                                Vehicle.controls_.Yaw +=
                                    TOUCH_SENSITIVITY * camera.Fov / graphics.Height * state.Delta.X;
                                Vehicle.controls_.Pitch +=
                                    TOUCH_SENSITIVITY * camera.Fov / graphics.Height * state.Delta.Y;
                            }
                        }
                    }
                    else
                    {
                        Vehicle.controls_.Yaw += input.MouseMoveX * YAW_SENSITIVITY;
                        Vehicle.controls_.Pitch += input.MouseMoveY * YAW_SENSITIVITY;
                    }

                    // Limit pitch
                    Vehicle.controls_.Pitch = MathDefs.Clamp(Vehicle.controls_.Pitch, 0.0f, 80.0f);
                    // Check for loading / saving the scene
                    //if (input.GetKeyPress(KEY_F5))
                    //{
                    //    File saveFile(Context, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/RaycastVehicleDemo.xml", FILE_WRITE);
                    //    Scene.SaveXML(saveFile);
                    //}
                    //if (input.GetKeyPress(KEY_F7))
                    //{
                    //    File loadFile(Context, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/RaycastVehicleDemo.xml", FILE_READ);
                    //    Scene.LoadXML(loadFile);
                    //    // After loading we have to reacquire the weak pointer to the Vehicle component, as it has been recreated
                    //    // Simply find the vehicle's scene node by name as there's only one of them
                    //    Node vehicleNode = Scene.GetChild("Vehicle2", true);
                    //    if (vehicleNode != null)
                    //    {
                    //        vehicle_ = vehicleNode.GetComponent<Vehicle2>();
                    //    }
                    //}
                }
                else
                {
                    Vehicle.controls_.Set(
                        Vehicle2.CTRL_FORWARD | Vehicle2.CTRL_BACK | Vehicle2.CTRL_LEFT | Vehicle2.CTRL_RIGHT |
                        Vehicle2.CTRL_BRAKE, false);
                }
            }
        }

        private void HandlePostUpdate(StringHash eventType, VariantMap eventData)
        {
            if (Vehicle == null) return;
            var vehicleNode = Vehicle.Node;
            // Physics update has completed. Position camera behind vehicle
            var dir = new Quaternion(vehicleNode.Rotation.YawAngle, Vector3.Up);
            dir = dir * new Quaternion(Vehicle.controls_.Yaw, Vector3.Up);
            dir = dir * new Quaternion(Vehicle.controls_.Pitch, Vector3.Right);
            var cameraTargetPos =
                vehicleNode.Position - dir * new Vector3(0.0f, 0.0f, CAMERA_DISTANCE);
            var cameraStartPos = vehicleNode.Position;
            // Raycast camera against static objects (physics collision mask 2)
            // and move it closer to the vehicle if something in between
            var cameraRay = new Ray(cameraStartPos, cameraTargetPos - cameraStartPos);
            var cameraRayLength = (cameraTargetPos - cameraStartPos).Length;
            var result = new PhysicsRaycastResult();
            Scene.GetComponent<PhysicsWorld>().RaycastSingle(result, cameraRay, cameraRayLength, 2);
            if (result.Body != null) cameraTargetPos = cameraStartPos + cameraRay.Direction * (result.Distance - 0.5f);
            CameraNode.Position = cameraTargetPos;
            CameraNode.Rotation = dir;
        }
    }
}