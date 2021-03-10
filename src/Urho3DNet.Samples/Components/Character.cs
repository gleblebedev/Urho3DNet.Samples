namespace Urho3DNet.Samples
{
    internal class Character : LogicComponent
    {
        public const uint CTRL_FORWARD = 1;
        public const uint CTRL_BACK = 2;
        public const uint CTRL_LEFT = 4;
        public const uint CTRL_RIGHT = 8;
        public const uint CTRL_JUMP = 16;

        public const float MOVE_FORCE = 0.8f;
        public const float INAIR_MOVE_FORCE = 0.02f;
        public const float BRAKE_FORCE = 0.2f;
        public const float JUMP_FORCE = 7.0f;
        public const float YAW_SENSITIVITY = 0.1f;
        public const float INAIR_THRESHOLD_TIME = 0.1f;

        /// Grounded flag for movement.
        [SerializeField(Name = "On Ground")] private bool onGround;

        /// Jump flag.
        [SerializeField(Name = "OK To Jump")] private bool okToJump = true;

        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        [SerializeField(Name = "In Air Timer")]
        private float inAirTimer;

        public Character(Context context) : base(context)
        {
            // Only the physics update event is needed: unsubscribe from the rest for optimization
            UpdateEventMask = UpdateEvent.UseFixedupdate;
        }

        /// Movement controls. Assigned by the main program each frame.
        public Controls Controls { get; set; } = new Controls();

        [SerializeField(Name = "Controls Yaw")]
        private float InternalControlsYaw
        {
            get => Controls.Yaw;
            set => Controls.Yaw = value;
        }

        [SerializeField(Name = "Controls Pitch")]
        private float InternalControlsPitch
        {
            get => Controls.Pitch;
            set => Controls.Pitch = value;
        }

        public new static void RegisterObject(Context context)
        {
            context.RegisterFactory<Character>();
        }

        public override void Start()
        {
            // Component has been inserted into its scene node. Subscribe to events now
            SubscribeToEvent(E.NodeCollision, Node, HandleNodeCollision);
        }

        public override void FixedUpdate(float timeStep)
        {
            /// \todo Could cache the components for faster access instead of finding them each frame
            var body = Node.GetComponent<RigidBody>();
            var animCtrl = Node.GetComponent<AnimationController>(true);

            // Update the in air timer. Reset if grounded
            if (!onGround)
                inAirTimer += timeStep;
            else
                inAirTimer = 0.0f;
            // When character has been in air less than 1/10 second, it's still interpreted as being on ground
            var softGrounded = inAirTimer < INAIR_THRESHOLD_TIME;

            // Update movement & animation
            var rot = Node.Rotation;
            var moveDir = Vector3.Zero;
            var velocity = body.LinearVelocity;
            // Velocity on the XZ plane
            var planeVelocity = new Vector3(velocity.X, 0.0f, velocity.Z);

            if (Controls.IsDown(CTRL_FORWARD))
                moveDir += Vector3.Forward;
            if (Controls.IsDown(CTRL_BACK))
                moveDir += Vector3.Back;
            if (Controls.IsDown(CTRL_LEFT))
                moveDir += Vector3.Left;
            if (Controls.IsDown(CTRL_RIGHT))
                moveDir += Vector3.Right;

            // Normalize move vector so that diagonal strafing is not faster
            if (moveDir.LengthSquared > 0.0f)
                moveDir.Normalize();

            // If in air, allow control, but slower than when on ground
            body.ApplyImpulse(rot * moveDir * (softGrounded ? MOVE_FORCE : INAIR_MOVE_FORCE));

            if (softGrounded)
            {
                // When on ground, apply a braking force to limit maximum ground velocity
                var brakeForce = -planeVelocity * BRAKE_FORCE;
                body.ApplyImpulse(brakeForce); // TODO: something is going wrong here

                // Jump. Must release jump control between jumps
                if (Controls.IsDown(CTRL_JUMP))
                {
                    if (okToJump)
                    {
                        body.ApplyImpulse(Vector3.Up * JUMP_FORCE);
                        okToJump = false;
                        animCtrl.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, false, 0.2f);
                    }
                }
                else
                {
                    okToJump = true;
                }
            }

            if (!onGround)
            {
                animCtrl.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, false, 0.2f);
            }
            else
            {
                // Play walk animation if moving on ground, otherwise fade it out
                if (softGrounded && !moveDir.Equals(Vector3.Zero))
                    animCtrl.PlayExclusive("Models/Mutant/Mutant_Run.ani", 0, true, 0.2f);
                else
                    animCtrl.PlayExclusive("Models/Mutant/Mutant_Idle0.ani", 0, true, 0.2f);

                // Set walk animation speed proportional to velocity
                animCtrl.SetSpeed("Models/Mutant/Mutant_Run.ani", planeVelocity.Length * 0.3f);
            }

            // Reset grounded flag for next frame
            onGround = false;
        }

        private void HandleNodeCollision(StringHash eventType, VariantMap eventData)
        {
            // Check collision contacts and see if character is standing on ground (look for a contact that has near vertical normal)
            var contacts = new MemoryBuffer(eventData[E.NodeCollision.Contacts].Buffer);

            while (!contacts.IsEof())
            {
                var contactPosition = contacts.ReadVector3();
                var contactNormal = contacts.ReadVector3();
                /*float contactDistance = */
                contacts.ReadFloat();
                /*float contactImpulse = */
                contacts.ReadFloat();

                // If contact is below node center and pointing up, assume it's a ground contact
                if (contactPosition.Y < Node.Position.Y + 1.0f)
                {
                    var level = contactNormal.Y;
                    if (level > 0.75)
                        onGround = true;
                }
            }
        }
    }
}