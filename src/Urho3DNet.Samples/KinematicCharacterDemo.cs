namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class KinematicCharacterDemo : Sample
    {
        private KinematicCharacter character_;
        private KinematicCharacterController kinematicCharacter_;
        private bool firstPerson_ = true;
        private bool drawDebug_;

        public KinematicCharacterDemo(Context context) : base(context)
        {
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            //if (touchEnabled_)
            //    touch_ = new Touch(context_, TOUCH_SENSITIVITY);

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
            //            auto* cache = GetSubsystem<ResourceCache>();

            //            Node* objectNode = scene_.CreateChild("Jack");
            //            objectNode.SetPosition(Vector3(0.0f, 1.0f, 0.0f));

            //            // spin node
            //            Node* adjustNode = objectNode.CreateChild("AdjNode");
            //            adjustNode.SetRotation(Quaternion(180, Vector3(0, 1, 0)));

            //            // Create the rendering component + animation controller
            //            auto * object = adjustNode.CreateComponent<AnimatedModel>();
            //            object.SetModel(cache.GetResource<Model>("Models/Mutant/Mutant.mdl"));
            //            object.SetMaterial(cache.GetResource<Material>("Models/Mutant/Materials/mutant_M.xml"));
            //            object.SetCastShadows(true);
            //            adjustNode.CreateComponent<AnimationController>();

            //            // Set the head bone for manual control
            //            object.GetSkeleton().GetBone("Mutant:Head").animated_ = false;

            //            // Create rigidbody
            //            auto* body = objectNode.CreateComponent<RigidBody>();
            //            body.SetCollisionLayer(1);
            //            body.SetKinematic(true);
            //            body.SetTrigger(true);

            //            // Set zero angular factor so that physics doesn't turn the character on its own.
            //            // Instead we will control the character yaw manually
            //            body.SetAngularFactor(Vector3::ZERO);

            //            // Set the rigidbody to signal collision also when in rest, so that we get ground collisions properly
            //            body.SetCollisionEventMode(COLLISION_ALWAYS);

            //            // Set a capsule shape for collision
            //            auto* shape = objectNode.CreateComponent<CollisionShape>();
            //            shape.SetCapsule(0.7f, 1.8f, Vector3(0.0f, 0.9f, 0.0f));

            //            // Create the character logic component, which takes care of steering the rigidbody
            //            // Remember it so that we can set the controls. Use a ea::weak_ptr because the scene hierarchy already owns it
            //            // and keeps it alive as long as it's not removed from the hierarchy
            //            character_ = objectNode.CreateComponent<KinematicCharacter>();
            //            kinematicCharacter_ = objectNode.CreateComponent<KinematicCharacterController>();
        }

        private void CreateInstructions()
        {
            //            auto* cache = GetSubsystem<ResourceCache>();
            //            auto* ui = GetSubsystem<UI>();

            //            // Construct new Text object, set string to display and font to use
            //            auto* instructionText = ui.GetRoot().CreateChild<Text>();
            //            instructionText.SetText(
            //                "Use WASD keys and mouse/touch to move\n"

            //                "Space to jump, F to toggle 1st/3rd person\n"

            //                "F5 to save scene, F7 to load"
            //            );
            //            instructionText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            //            // The text has multiple rows. Center them in relation to each other
            //            instructionText.SetTextAlignment(HA_CENTER);

            //            // Position the text relative to the screen center
            //            instructionText.SetHorizontalAlignment(HA_CENTER);
            //            instructionText.SetVerticalAlignment(VA_CENTER);
            //            instructionText.SetPosition(0, ui.GetRoot().GetHeight() / 4);
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

            if (character_ != null)
            {
            //        // Clear previous controls
            //        character_.controls_.Set(CTRL_FORWARD | CTRL_BACK | CTRL_LEFT | CTRL_RIGHT | CTRL_JUMP, false);

            //        // Update controls using touch utility class
            //        if (touch_)
            //                    touch_.UpdateTouches(character_.controls_);

            //    // Update controls using keys
            //    auto* ui = GetSubsystem<UI>();
            //        if (!ui.GetFocusElement())
            //        {
            //            if (!touch_ || !touch_.useGyroscope_)
            //            {
            //                character_.controls_.Set(CTRL_FORWARD, input.GetKeyDown(KEY_W));
            //                character_.controls_.Set(CTRL_BACK, input.GetKeyDown(KEY_S));
            //                character_.controls_.Set(CTRL_LEFT, input.GetKeyDown(KEY_A));
            //                character_.controls_.Set(CTRL_RIGHT, input.GetKeyDown(KEY_D));
            //            }
            //    character_.controls_.Set(CTRL_JUMP, input.GetKeyDown(KEY_SPACE));

            //            // Add character yaw & pitch from the mouse motion or touch input
            //            if (touchEnabled_)
            //            {
            //                for (unsigned i = 0; i<input.GetNumTouches(); ++i)
            //                {
            //                    TouchState* state = input.GetTouch(i);
            //                    if (!state.touchedElement_)    // Touch on empty space
            //                    {
            //                        auto* camera = cameraNode_.GetComponent<Camera>();
            //                        if (!camera)
            //                            return;

            //                        auto* graphics = GetSubsystem<Graphics>();
            //    character_.controls_.yaw_ += TOUCH_SENSITIVITY* camera.GetFov() / graphics.GetHeight() * state.delta_.x_;
            //                        character_.controls_.pitch_ += TOUCH_SENSITIVITY* camera.GetFov() / graphics.GetHeight() * state.delta_.y_;
            //                    }
            //                }
            //            }
            //            else
            //{
            //    character_.controls_.yaw_ += (float)input.GetMouseMoveX() * YAW_SENSITIVITY;
            //    character_.controls_.pitch_ += (float)input.GetMouseMoveY() * YAW_SENSITIVITY;
            //}
            //// Limit pitch
            //character_.controls_.pitch_ = Clamp(character_.controls_.pitch_, -80.0f, 80.0f);
            //// Set rotation already here so that it's updated every rendering frame instead of every physics frame
            //character_.GetNode().SetRotation(Quaternion(character_.controls_.yaw_, Vector3::UP));

            //// Switch between 1st and 3rd person
            //if (input.GetKeyPress(KEY_F))
            //    firstPerson_ = !firstPerson_;

            //// Turn on/off gyroscope on mobile platform
            //if (touch_ && input.GetKeyPress(KEY_G))
            //    touch_.useGyroscope_ = !touch_.useGyroscope_;

            //// Check for loading / saving the scene
            //if (input.GetKeyPress(KEY_F5))
            //{
            //    File saveFile(context_, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CharacterDemo.xml", FILE_WRITE);
            //    scene_.SaveXML(saveFile);
            //}
            //if (input.GetKeyPress(KEY_F7))
            //{
            //    File loadFile(context_, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CharacterDemo.xml", FILE_READ);
            //    scene_.LoadXML(loadFile);
            //    // After loading we have to reacquire the weak pointer to the Character component, as it has been recreated
            //    // Simply find the character's scene node by name as there's only one of them
            //    Node* characterNode = scene_.GetChild("Jack", true);
            //    if (characterNode)
            //        character_ = characterNode.GetComponent<KinematicCharacter>();
            //}
            //        }
            }

            // Toggle debug geometry with space
            if (input.GetKeyPress(Key.KeyM))
                drawDebug_ = !drawDebug_;
        }

        private void HandlePostUpdate(VariantMap eventData)
        {
            if (character_ == null)
                return;

            Node characterNode = character_.Node;

            // Get camera lookat dir from character yaw + pitch
            Quaternion rot = characterNode.Rotation;
            //    Quaternion dir = rot * Quaternion(character_.controls_.pitch_, Vector3::RIGHT);

            //    // Turn head to camera pitch, but limit to avoid unnatural animation
            //    Node* headNode = characterNode.GetChild("Mutant:Head", true);
            //    float limitPitch = Clamp(character_.controls_.pitch_, -45.0f, 45.0f);
            //    Quaternion headDir = rot * Quaternion(limitPitch, Vector3(1.0f, 0.0f, 0.0f));
            //    // This could be expanded to look at an arbitrary target, now just look at a point in front
            //    Vector3 headWorldTarget = headNode.GetWorldPosition() + headDir * Vector3(0.0f, 0.0f, -1.0f);
            //    headNode.LookAt(headWorldTarget, Vector3(0.0f, 1.0f, 0.0f));

            //if (firstPerson_)
            //{
            //    CameraNode.Position = (headNode.GetWorldPosition() + rot * Vector3(0.0f, 0.15f, 0.2f));
            //    CameraNode.Rotation = (dir);
            //}
            //else
            //{
            //    // Third person camera: position behind the character
            //    Vector3 aimPoint = characterNode.GetPosition() + rot * Vector3(0.0f, 1.7f, 0.0f);

            //    // Collide camera ray with static physics objects (layer bitmask 2) to ensure we see the character properly
            //    Vector3 rayDir = dir * Vector3::BACK;
            //    float rayDistance = touch_ ? touch_.cameraDistance_ : CAMERA_INITIAL_DIST;
            //    PhysicsRaycastResult result;
            //    scene_.GetComponent<PhysicsWorld>().RaycastSingle(result, Ray(aimPoint, rayDir), rayDistance, 2);
            //    if (result.body_)
            //        rayDistance = Min(rayDistance, result.distance_);
            //    rayDistance = Clamp(rayDistance, CAMERA_MIN_DIST, CAMERA_MAX_DIST);

            //    cameraNode_.SetPosition(aimPoint + rayDir * rayDistance);
            //    cameraNode_.SetRotation(dir);
            //}
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