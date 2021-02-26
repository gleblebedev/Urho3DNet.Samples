using System;

namespace Urho3DNet.Samples
{
    public class KinematicCharacter: LogicComponent
    {
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

            Node node_;
            Matrix3x4 transform_;
        };

    }
}