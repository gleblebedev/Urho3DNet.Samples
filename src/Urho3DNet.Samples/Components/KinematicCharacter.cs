using System;

namespace Urho3DNet.Samples
{
    //[ObjectFactory]
    public class KinematicCharacter : LogicComponent
    {
        public const uint CTRL_FORWARD = 1;
        public const uint CTRL_BACK = 2;
        public const uint CTRL_LEFT = 4;
        public const uint CTRL_RIGHT = 8;
        public const uint CTRL_JUMP = 16;
        public const float YAW_SENSITIVITY = 0.1f;

        private const float MOVE_FORCE = 0.2f;
        private const float INAIR_MOVE_FORCE = 0.2f;
        private const float BRAKE_FORCE = 0.2f;
        private const float JUMP_FORCE = 7.0f;
        private const float INAIR_THRESHOLD_TIME = 0.1f;

        // moving platform data
        private readonly MovingData[] movingData_ = new MovingData[2] {MovingData.Identity, MovingData.Identity};

        /// Grounded flag for movement.
        private bool onGround_;

        /// Jump flag.
        private bool okToJump_;

        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        private float inAirTimer_;

        // extra vars
        private Vector3 curMoveDir_;
        private bool isJumping_;
        private bool jumpStarted_;

        private AnimationController animController_;
        private KinematicCharacterController kinematicController_;

        public KinematicCharacter(Context context) : base(context)
        {
            // Only the physics update event is needed: unsubscribe from the rest for optimization
            UpdateEventMask = UpdateEvent.UseFixedupdate | UpdateEvent.UseFixedpostupdate;
        }

        public Controls Controls { get; set; } = new Controls();

        public override void DelayedStart()
        {
            animController_ = Node.GetComponent<AnimationController>(true);
            kinematicController_ = Node.GetComponent<KinematicCharacterController>(true);
        }

        public override void Start()
        {
            // Component has been inserted into its scene node. Subscribe to events now
            SubscribeToEvent(E.NodeCollision, Node, HandleNodeCollision);
        }

        public override void FixedUpdate(float timeStep)
        {
            // Update the in air timer. Reset if grounded
            if (!onGround_)
                inAirTimer_ += timeStep;
            else
                inAirTimer_ = 0.0f;
            // When character has been in air less than 1/10 second, it's still interpreted as being on ground
            var softGrounded = inAirTimer_ < INAIR_THRESHOLD_TIME;

            // Update movement & animation
            var rot = Node.Rotation;
            var moveDir = Vector3.Zero;
            onGround_ = kinematicController_.OnGround();

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

            // rotate movedir
            var velocity = rot * moveDir;
            if (onGround_)
                curMoveDir_ = velocity;
            else
                // In-air direction control is limited
                curMoveDir_ = curMoveDir_.Lerp(velocity, 0.03f);

            kinematicController_.SetWalkIncrement(curMoveDir_ * (softGrounded ? MOVE_FORCE : INAIR_MOVE_FORCE));

            if (softGrounded)
            {
                if (isJumping_) isJumping_ = false;
                isJumping_ = false;
                // Jump. Must release jump control between jumps
                if (Controls.IsDown(CTRL_JUMP))
                {
                    isJumping_ = true;
                    if (okToJump_)
                    {
                        okToJump_ = false;
                        jumpStarted_ = true;
                        kinematicController_.Jump();

                        animController_.StopLayer(0);
                        animController_.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, false, 0.2f);
                        animController_.SetTime("Models/Mutant/Mutant_Jump1.ani", 0);
                    }
                }
                else
                {
                    okToJump_ = true;
                }
            }

            if (!onGround_ || jumpStarted_)
            {
                if (jumpStarted_)
                {
                    animController_.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.3f);
                    animController_.SetTime("Models/Mutant/Mutant_Jump1.ani", 0);
                    jumpStarted_ = false;
                }
                else
                {
                    const float maxDistance = 50.0f;
                    const float segmentDistance = 10.01f;
                    var result = new PhysicsRaycastResult();
                    Scene.GetComponent<PhysicsWorld>().RaycastSingleSegmented(result,
                        new Ray(Node.Position, Vector3.Down),
                        maxDistance, segmentDistance, 0xffff);
                    if (result.Body != null && result.Distance > 0.7f)
                        animController_.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.2f);
                }
            }
            else
            {
                // Play walk animation if moving on ground, otherwise fade it out
                if (softGrounded && !moveDir.Equals(Vector3.Zero))
                    animController_.PlayExclusive("Models/Mutant/Mutant_Run.ani", 0, true, 0.2f);
                else
                    animController_.PlayExclusive("Models/Mutant/Mutant_Idle0.ani", 0, true, 0.2f);
            }
        }

        public override void FixedPostUpdate(float timeStep)
        {
            //if (movingData_[0] == movingData_[1])
            //{
            //    var delta = movingData_[0].transform_ * movingData_[1].transform_.Inverse();

            //    // add delta
            //    Vector3 kPos = default;
            //    Quaternion kRot = default;
            //    kinematicController_.GetTransform(ref kPos, ref kRot);
            //    var matKC = new Matrix3x4(kPos, kRot, Vector3.One);

            //    // update
            //    matKC = delta * matKC;
            //    kinematicController_.SetTransform(matKC.Translation, matKC.Rotation);

            //    // update yaw control (directly rotates char)
            //    Controls.Yaw += delta.Rotation.YawAngle;
            //}

            // update node position
            //var nodeWorldPosition = kinematicController_.GetPosition();
            //if (nodeWorldPosition.IsNaN)
            //    throw new ApplicationException("Wrong position!");
            //Node.WorldPosition = nodeWorldPosition;

            // shift and clear
            movingData_[1] = movingData_[0];
            movingData_[0].node_ = null;
        }

        private bool IsNodeMovingPlatform(Node node)
        {
            if (node == null) return false;

            var var = node.GetVar("IsMovingPlatform");
            return var != Variant.Empty && var.Bool;
        }

        private void NodeOnMovingPlatform(Node node)
        {
            if (!IsNodeMovingPlatform(node)) return;

            movingData_[0].node_ = node;
            movingData_[0].transform_ = node.WorldTransform;
        }

        private void HandleNodeCollision(VariantMap eventData)
        {
            // Check collision contacts and see if character is standing on ground (look for a contact that has near vertical normal)

            // possible moving platform trigger volume
            var rigidBody = (RigidBody) eventData[E.NodeCollision.OtherBody].Ptr;
            if (rigidBody.IsTrigger) NodeOnMovingPlatform((Node) eventData[E.NodeCollision.OtherNode].Ptr);
        }


        public struct MovingData : IEquatable<MovingData>
        {
            public static readonly MovingData Identity = new MovingData(null, Matrix3x4.Identity);

            public bool Equals(MovingData other)
            {
                return Equals(node_, other.node_);
            }

            public override bool Equals(object obj)
            {
                return obj is MovingData other && Equals(other);
            }

            public override int GetHashCode()
            {
                return node_ != null ? node_.GetHashCode() : 0;
            }

            public static bool operator ==(MovingData left, MovingData right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(MovingData left, MovingData right)
            {
                return !left.Equals(right);
            }

            public MovingData(Node n, Matrix3x4 transform)
            {
                node_ = n;
                transform_ = transform;
            }

            public Node node_;
            public Matrix3x4 transform_;
        }
    }
}