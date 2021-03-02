using System;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class KinematicCharacterDemo : Sample
    {
        /// Touch utility object.
        private Touch touch_;

        /// The controllable character component.
        private KinematicCharacter character_;
        private KinematicCharacterController kinematicCharacter_;
        /// First person camera flag.
        private bool firstPerson_ = true;
        private bool drawDebug_;

        public KinematicCharacterDemo(Context context) : base(context)
        {
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            if (touchEnabled_)
                touch_ = new Touch(Context, TOUCH_SENSITIVITY);

            // Create static scene content
            CreateScene();

            // Create the controllable character
            CreateCharacter();

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
            CameraNode.Position = new Vector3(0.0f, 1.5f);
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 300.0f;
            GetSubsystem<Renderer>().SetViewport(0, new Viewport(Context, Scene, camera));

            // Create static scene content. First create a zone for ambient lighting and fog control
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            zone.AmbientColor = new Color(0.15f, 0.15f, 0.15f);
            zone.FogColor = new Color(0.5f, 0.5f, 0.7f);
            zone.FogStart = 100.0f;
            zone.FogEnd = 300.0f;
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));

            // Create a directional light with cascaded shadow mapping
            var lightNode = Scene.CreateChild("DirectionalLight");
            lightNode.Direction = new Vector3(0.3f, -0.5f, 0.425f);
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.CastShadows = true;
            light.ShadowBias = new BiasParameters(0.00025f, 0.5f);
            light.ShadowCascade = new CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f);
            light.SpecularIntensity = 0.5f;

            // Create the floor object
            var floorNode = Scene.CreateChild("Floor");
            floorNode.Position = new Vector3(0.0f, -0.5f);
            floorNode.SetScale(new Vector3(200.0f, 1.0f, 200.0f));
            var obj = floorNode.CreateComponent<StaticModel>();
            obj.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
            obj.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));

            {
                var body = floorNode.CreateComponent<RigidBody>();
                // Use collision layer bit 2 to mark world scenery. This is what we will raycast against to prevent camera from going
                // inside geometry
                body.CollisionLayer = 2;
                var shape = floorNode.CreateComponent<CollisionShape>();
                shape.SetBox(Vector3.One);
            }

            // Create mushrooms of varying sizes
            const int NUM_MUSHROOMS = 60;
            for (var i = 0; i < NUM_MUSHROOMS; ++i)
            {
                var objectNode = Scene.CreateChild("Mushroom");
                objectNode.Position =
                    new Vector3(MathDefs.Random(180.0f) - 90.0f, 0.0f, MathDefs.Random(180.0f) - 90.0f);
                objectNode.Rotation = new Quaternion(0.0f, MathDefs.Random(360.0f), 0.0f);
                objectNode.SetScale(2.0f + MathDefs.Random(5.0f));
                var @object = objectNode.CreateComponent<StaticModel>();
                @object.SetModel(cache.GetResource<Model>("Models/Mushroom.mdl"));
                @object.SetMaterial(cache.GetResource<Material>("Materials/Mushroom.xml"));
                @object.CastShadows = true;

                var body = objectNode.CreateComponent<RigidBody>();
                body.CollisionLayer = 2;
                var shape = objectNode.CreateComponent<CollisionShape>();
                shape.SetTriangleMesh(@object.GetModel(), 0);
            }

            // Create movable boxes. Let them fall from the sky at first
            const int NUM_BOXES = 100;
            for (var i = 0; i < NUM_BOXES; ++i)
            {
                var scale = MathDefs.Random(2.0f) + 0.5f;

                var objectNode = Scene.CreateChild("Box");
                objectNode.Position = new Vector3(MathDefs.Random(180.0f) - 90.0f, MathDefs.Random(10.0f) + 10.0f,
                    MathDefs.Random(180.0f) - 90.0f);
                objectNode.Rotation = new Quaternion(MathDefs.Random(360.0f), MathDefs.Random(360.0f),
                    MathDefs.Random(360.0f));
                objectNode.SetScale(scale);
                var @object = objectNode.CreateComponent<StaticModel>();
                @object.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
                @object.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));
                @object.CastShadows = true;

                var body = objectNode.CreateComponent<RigidBody>();
                body.CollisionLayer = 2;
                // Bigger boxes will be heavier and harder to move
                body.Mass = scale * 2.0f;
                var shape = objectNode.CreateComponent<CollisionShape>();
                shape.SetBox(Vector3.One);
            }
        }

        private void CreateCharacter()
        {
            var cache = GetSubsystem<ResourceCache>();

            Node objectNode = Scene.CreateChild("Jack");
            objectNode.Position = new Vector3(0.0f, 1.0f, 0.0f);

            // spin node
            Node adjustNode = objectNode.CreateChild("AdjNode");
            adjustNode.Rotation = new Quaternion(180, new Vector3(0, 1, 0));

            // Create the rendering component + animation controller
            var @object = adjustNode.CreateComponent<AnimatedModel>();
            @object.SetModel(cache.GetResource<Model>("Models/Mutant/Mutant.mdl"), true);
            @object.SetMaterial(cache.GetResource<Material>("Models/Mutant/Materials/mutant_M.xml"));
            @object.CastShadows = true;
            adjustNode.CreateComponent<AnimationController>();

            // Set the head bone for manual control
            @object.Skeleton.GetBone("Mutant:Head").Animated = false;

            // Create rigidbody
            var body = objectNode.CreateComponent<RigidBody>();
            body.CollisionLayer = (1);
            body.IsKinematic = (true);
            body.IsTrigger = (true);

            // Set zero angular factor so that physics doesn't turn the character on its own.
            // Instead we will control the character yaw manually
            body.AngularFactor = Vector3.Zero;

            // Set the rigidbody to signal collision also when in rest, so that we get ground collisions properly
            body.CollisionEventMode = CollisionEventMode.CollisionAlways;

            // Set a capsule shape for collision
            var shape = objectNode.CreateComponent<CollisionShape>();
            shape.SetCapsule(0.7f, 1.8f, new Vector3(0.0f, 0.9f, 0.0f));

            // Create the character logic component, which takes care of steering the rigidbody
            // Remember it so that we can set the controls. Use a ea::weak_ptr because the scene hierarchy already owns it
            // and keeps it alive as long as it's not removed from the hierarchy
            character_ = objectNode.CreateComponent<KinematicCharacter>();
            kinematicCharacter_ = objectNode.CreateComponent<KinematicCharacterController>();
        }

        private void CreateInstructions()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();

            // Construct new Text object, set string to display and font to use
            var instructionText = ui.Root.CreateChild<Text>();
            instructionText.SetText(
                "Use WASD keys and mouse/touch to move\n"+
                "Space to jump, F to toggle 1st/3rd person\n"+
                "F5 to save scene, F7 to load"
            );
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
            // Subscribe to Update event for setting the character controls before physics simulation
            SubscribeToEvent(E.Update, HandleUpdate);

            // Subscribe to PostUpdate event for updating the camera position after physics simulation
            SubscribeToEvent(E.PostUpdate, HandlePostUpdate);

            // Unsubscribe the SceneUpdate event from base class as the camera node is being controlled in HandlePostUpdate() in this sample
            UnsubscribeFromEvent(E.SceneUpdate);
        }

        private void HandleUpdate(VariantMap eventData)
        {
            var input = GetSubsystem<Input>();

            if (character_ != null && character_.IsNotExpired)
            {
                // Clear previous controls
                character_.Controls.Set(KinematicCharacter.CTRL_FORWARD | KinematicCharacter.CTRL_BACK | KinematicCharacter.CTRL_LEFT | KinematicCharacter.CTRL_RIGHT | KinematicCharacter.CTRL_JUMP, false);

                // Update controls using touch utility class
                if (touch_ != null)
                    touch_.UpdateTouches(character_.Controls);

                // Update controls using keys
                var ui = GetSubsystem<UI>();
                if (ui.GetFocusElement() == null)
                {
                    if (touch_ == null || touch_.UseGyroscope == false)
                    {
                        character_.Controls.Set(KinematicCharacter.CTRL_FORWARD, input.GetKeyDown(Key.KeyW));
                        character_.Controls.Set(KinematicCharacter.CTRL_BACK, input.GetKeyDown(Key.KeyS));
                        character_.Controls.Set(KinematicCharacter.CTRL_LEFT, input.GetKeyDown(Key.KeyA));
                        character_.Controls.Set(KinematicCharacter.CTRL_RIGHT, input.GetKeyDown(Key.KeyD));
                    }
                    character_.Controls.Set(KinematicCharacter.CTRL_JUMP, input.GetKeyDown(Key.KeySpace));

                    // Add character yaw & pitch from the mouse motion or touch input
                    if (touchEnabled_)
                    {
                        for (uint i = 0; i < input.NumTouches; ++i)
                        {
                            TouchState state = input.GetTouch(i);
                            if (state.TouchedElement == null)    // Touch on empty space
                            {
                                var camera = CameraNode.GetComponent<Camera>();
                                if (camera == null)
                                    return;

                                var graphics = GetSubsystem<Graphics>();
                                character_.Controls.Yaw += TOUCH_SENSITIVITY * camera.Fov / graphics.Height * state.Delta.X;
                                character_.Controls.Pitch += TOUCH_SENSITIVITY * camera.Fov / graphics.Height * state.Delta.Y;
                            }
                        }
                    }
                    else
                    {
                        character_.Controls.Yaw += (float)input.MouseMoveX * KinematicCharacter.YAW_SENSITIVITY;
                        character_.Controls.Pitch += (float)input.MouseMoveY * KinematicCharacter.YAW_SENSITIVITY;
                    }
                    // Limit pitch
                    character_.Controls.Pitch = MathDefs.Clamp(character_.Controls.Pitch, -80.0f, 80.0f);
                    // Set rotation already here so that it's updated every rendering frame instead of every physics frame
                    character_.Node.Rotation = new Quaternion(character_.Controls.Yaw, Vector3.Up);

                    // Switch between 1st and 3rd person
                    if (input.GetKeyPress(Key.KeyF))
                        firstPerson_ = !firstPerson_;

                    // Turn on/off gyroscope on mobile platform
                    if (touch_ != null && input.GetKeyPress(Key.KeyG))
                        touch_.UseGyroscope = !touch_.UseGyroscope;

                    // Check for loading / saving the scene
                    if (input.GetKeyPress(Key.KeyF5))
                    {
                        //File saveFile(context_, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CharacterDemo.xml", FILE_WRITE);
                        //scene_.SaveXML(saveFile);
                    }
                    if (input.GetKeyPress(Key.KeyF7))
                    {
                        //File loadFile(context_, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CharacterDemo.xml", FILE_READ);
                        //scene_.LoadXML(loadFile);
                        //// After loading we have to reacquire the weak pointer to the Character component, as it has been recreated
                        //// Simply find the character's scene node by name as there's only one of them
                        //Node* characterNode = scene_.GetChild("Jack", true);
                        //if (characterNode)
                        //    character_ = characterNode.GetComponent<KinematicCharacter>();
                    }
                }
            }

            // Toggle debug geometry with space
            if (input.GetKeyPress(Key.KeyM))
                drawDebug_ = !drawDebug_;
        }

        private void HandlePostUpdate(VariantMap eventData)
        {
            if (character_ == null || character_.IsExpired)
                return;

            var characterNode = character_.Node;

            // Get camera lookat dir from character yaw + pitch
            var rot = characterNode.Rotation;
            Quaternion dir = rot * new Quaternion(character_.Controls.Pitch, Vector3.Right);

            // Turn head to camera pitch, but limit to avoid unnatural animation
            var headNode = characterNode.GetChild("Mutant:Head", true);
            float limitPitch = MathDefs.Clamp(character_.Controls.Pitch, -45.0f, 45.0f);
            Quaternion headDir = rot * new Quaternion(limitPitch, new Vector3(1.0f, 0.0f, 0.0f));
            // This could be expanded to look at an arbitrary target, now just look at a point in front
            // TODO
            // Vector3 headWorldTarget = headNode.WorldPosition + headDir * new Vector3(0.0f, 0.0f, -1.0f);
            // headNode.LookAt(headWorldTarget, new Vector3(0.0f, 1.0f, 0.0f));

            if (firstPerson_)
            {
                CameraNode.Position = headNode.WorldPosition + rot * new Vector3(0.0f, 0.15f, 0.2f);
                CameraNode.Rotation = dir;
            }
            else
            {
                // Third person camera: position behind the character
                Vector3 aimPoint = characterNode.Position + rot * new Vector3(0.0f, 1.7f, 0.0f);

                // Collide camera ray with static physics objects (layer bitmask 2) to ensure we see the character properly
                Vector3 rayDir = dir * Vector3.Back;
                float rayDistance = touch_ != null ? touch_.CameraDistance : Touch.CAMERA_INITIAL_DIST;
                var result = new PhysicsRaycastResult();
                Scene.GetComponent<PhysicsWorld>().RaycastSingle(result, new Ray(aimPoint, rayDir), rayDistance, 2);
                if (result.Body != null)
                    rayDistance = Math.Min(rayDistance, result.Distance);
                rayDistance = MathDefs.Clamp(rayDistance, Touch.CAMERA_MIN_DIST, Touch.CAMERA_MAX_DIST);

                CameraNode.Position = aimPoint + rayDir * rayDistance;
                CameraNode.Rotation = dir;
            }
        }


        private void HandlePostRenderUpdate(VariantMap eventData)
        {
            if (drawDebug_)
            {
                Scene.GetComponent<PhysicsWorld>().DrawDebugGeometry(true);
                DebugRenderer dbgRenderer = Scene.GetComponent<DebugRenderer>();

                Node objectNode = Scene.GetChild("Player");
                if (objectNode != null)
                {
                    dbgRenderer.AddSphere(new Sphere(objectNode.WorldPosition, 0.1f), Color.Yellow);
                }
            }
        }
    }
}