using System;

namespace Urho3DNet.Samples
{
    public class KinematicCharacter: LogicComponent
    {
        const uint CTRL_FORWARD = 1;
        const uint CTRL_BACK = 2;
        const uint CTRL_LEFT = 4;
        const uint CTRL_RIGHT = 8;
        const uint CTRL_JUMP = 16;

        const float MOVE_FORCE = 0.2f;
        const float INAIR_MOVE_FORCE = 0.2f;
        const float BRAKE_FORCE = 0.2f;
        const float JUMP_FORCE = 7.0f;
        const float YAW_SENSITIVITY = 0.1f;
        const float INAIR_THRESHOLD_TIME = 0.1f;

        /// Grounded flag for movement.
        bool onGround_;
        /// Jump flag.
        bool okToJump_;
        /// In air timer. Due to possible physics inaccuracy, character can be off ground for max. 1/10 second and still be allowed to move.
        float inAirTimer_;

        // extra vars
        Vector3 curMoveDir_;
        bool isJumping_;
        bool jumpStarted_;

        CollisionShape collisionShape_;
        AnimationController animController_;
        KinematicCharacterController kinematicController_;

        // moving platform data
        MovingData[] movingData_ = new MovingData[2];

        public KinematicCharacter(Context context) : base(context)
        {
            // Only the physics update event is needed: unsubscribe from the rest for optimization
            UpdateEventMask = UpdateEvent.UseFixedupdate | UpdateEvent.UseFixedpostupdate;
        }

        public Controls Controls { get; set; } = new Controls();

        void DelayedStart()
        {
            collisionShape_ = Node.GetComponent<CollisionShape>(true);
            animController_ = Node.GetComponent<AnimationController>(true);
            kinematicController_ = Node.GetComponent<KinematicCharacterController>(true);
        }
        
        void Start()
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
            bool softGrounded = inAirTimer_ < INAIR_THRESHOLD_TIME;

            // Update movement & animation
            Quaternion rot = Node.Rotation;
            Vector3 moveDir = Vector3.Zero;
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
            Vector3 velocity = rot * moveDir;
            if (onGround_)
            {
                curMoveDir_ = velocity;
            }
            else
            {   // In-air direction control is limited
                curMoveDir_ = curMoveDir_.Lerp(velocity, 0.03f);
            }

            kinematicController_.SetWalkDirection(curMoveDir_ * (softGrounded ? MOVE_FORCE : INAIR_MOVE_FORCE));

            if (softGrounded)
            {
                if (isJumping_)
                {
                    isJumping_ = false;
                }
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
                    if (animController_.IsAtEnd("Models/Mutant/Mutant_Jump1.ani"))
                    {
                        animController_.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.3f);
                        animController_.SetTime("Models/Mutant/Mutant_Jump1.ani", 0);
                        jumpStarted_ = false;
                    }
                }
                else
                {
                    const float maxDistance = 50.0f;
                    const float segmentDistance = 10.01f;
                    PhysicsRaycastResult result = new PhysicsRaycastResult();
                    Scene.GetComponent<PhysicsWorld>().RaycastSingleSegmented(result, new Ray(Node.Position, Vector3.Down),
                                                                                     maxDistance, segmentDistance, 0xffff);
                    if (result.Body != null && result.Distance > 0.7f)
                    {
                        animController_.PlayExclusive("Models/Mutant/Mutant_Jump1.ani", 0, true, 0.2f);
                    }
                }
            }
            else
            {
                // Play walk animation if moving on ground, otherwise fade it out
                if ((softGrounded) && !moveDir.Equals(Vector3.Zero))
                {
                    animController_.PlayExclusive("Models/Mutant/Mutant_Run.ani", 0, true, 0.2f);
                }
                else
                {
                    animController_.PlayExclusive("Models/Mutant/Mutant_Idle0.ani", 0, true, 0.2f);
                }
            }
        }

        public override void FixedPostUpdate(float timeStep)
        {
            if (movingData_[0] == movingData_[1])
            {
                Matrix3x4 delta = movingData_[0].transform_ * movingData_[1].transform_.Inverse();

                // add delta
                Vector3 kPos = default;
                Quaternion kRot = default;
                kinematicController_.GetTransform(ref kPos, ref kRot);
                Matrix3x4 matKC = new Matrix3x4(kPos, kRot, Vector3.One);

                // update
                matKC = delta * matKC;
                kinematicController_.SetTransform(matKC.Translation, matKC.Rotation);

                // update yaw control (directly rotates char)
                Controls.Yaw += delta.Rotation.YawAngle;
            }

            // update node position
            Node.WorldPosition = kinematicController_.GetPosition();

            // shift and clear
            movingData_[1] = movingData_[0];
            movingData_[0].node_ = null;
        }

        bool IsNodeMovingPlatform(Node node)
        {
            if (node == null)
            {
                return false;
            }

            Variant @var = node.GetVar("IsMovingPlatform");
            return (@var != Variant.Empty && var.Bool);
        }

    void NodeOnMovingPlatform(Node node)
        {
            if (!IsNodeMovingPlatform(node))
            {
                return;
            }

            movingData_[0].node_ = node;
            movingData_[0].transform_ = node.WorldTransform;
        }

        void HandleNodeCollision(VariantMap eventData)
        {
            // Check collision contacts and see if character is standing on ground (look for a contact that has near vertical normal)

            // possible moving platform trigger volume
            var rigidBody = (RigidBody)eventData[E.NodeCollision.OtherBody].Ptr;
            if (rigidBody.IsTrigger)
            {
                NodeOnMovingPlatform((Node) eventData[E.NodeCollision.OtherNode].Ptr);
            }
        }


public struct MovingData : IEquatable<MovingData>
        {
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
                return (node_ != null ? node_.GetHashCode() : 0);
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
        };

    }
}