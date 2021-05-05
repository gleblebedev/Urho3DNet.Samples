using System;
using System.Diagnostics;
using System.Linq;

namespace Urho3DNet.Samples
{
    [ObjectFactory]
    public class CreateRagdoll : Component
    {
        public CreateRagdoll(Context context) : base(context)
        {
        }

        protected override void OnNodeSet(Node node)
        {
            // If the node pointer is non-null, this component has been created into a scene node. Subscribe to physics collisions that
            // concern this scene node
            if (node != null)
                SubscribeToEvent(E.NodeCollision, node, HandleNodeCollision);
        }

        private void HandleNodeCollision(VariantMap eventData)
        {
            // Get the other colliding body, make sure it is moving (has nonzero mass)
            var otherBody = eventData[E.NodeCollision.OtherBody].Ptr as RigidBody;

            if (otherBody != null && otherBody.Mass > 0.0f)
            {
                // We do not need the physics components in the AnimatedModel's root scene node anymore
                Node.RemoveComponent(typeof(RigidBody).Name);
                Node.RemoveComponent(typeof(CollisionShape).Name);

                // Create RigidBody & CollisionShape components to bones
                CreateRagdollBone("Bip01_Pelvis", ShapeType.ShapeBox, new Vector3(0.3f, 0.2f, 0.25f), new Vector3(0.0f),
                    new Quaternion(0.0f, 0.0f, 0.0f));
                CreateRagdollBone("Bip01_Spine1", ShapeType.ShapeBox, new Vector3(0.35f, 0.2f, 0.3f),
                    new Vector3(0.15f),
                    new Quaternion(0.0f, 0.0f, 0.0f));
                CreateRagdollBone("Bip01_L_Thigh", ShapeType.ShapeCapsule, new Vector3(0.175f, 0.45f, 0.175f),
                    new Vector3(0.25f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_R_Thigh", ShapeType.ShapeCapsule, new Vector3(0.175f, 0.45f, 0.175f),
                    new Vector3(0.25f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_L_Calf", ShapeType.ShapeCapsule, new Vector3(0.15f, 0.55f, 0.15f),
                    new Vector3(0.25f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_R_Calf", ShapeType.ShapeCapsule, new Vector3(0.15f, 0.55f, 0.15f),
                    new Vector3(0.25f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_Head", ShapeType.ShapeBox, new Vector3(0.2f, 0.2f, 0.2f), new Vector3(0.1f),
                    new Quaternion(0.0f, 0.0f, 0.0f));
                CreateRagdollBone("Bip01_L_UpperArm", ShapeType.ShapeCapsule, new Vector3(0.15f, 0.35f, 0.15f),
                    new Vector3(0.1f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_R_UpperArm", ShapeType.ShapeCapsule, new Vector3(0.15f, 0.35f, 0.15f),
                    new Vector3(0.1f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_L_Forearm", ShapeType.ShapeCapsule, new Vector3(0.125f, 0.4f, 0.125f),
                    new Vector3(0.2f),
                    new Quaternion(0.0f, 0.0f, 90.0f));
                CreateRagdollBone("Bip01_R_Forearm", ShapeType.ShapeCapsule, new Vector3(0.125f, 0.4f, 0.125f),
                    new Vector3(0.2f),
                    new Quaternion(0.0f, 0.0f, 90.0f));

                // Create Constraints between bones
                CreateRagdollConstraint("Bip01_L_Thigh", "Bip01_Pelvis", ConstraintType.ConstraintConetwist,
                    Vector3.Back, Vector3.Forward,
                    new Vector2(45.0f, 45.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_R_Thigh", "Bip01_Pelvis", ConstraintType.ConstraintConetwist,
                    Vector3.Back, Vector3.Forward,
                    new Vector2(45.0f, 45.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_L_Calf", "Bip01_L_Thigh", ConstraintType.ConstraintHinge,
                    Vector3.Back, Vector3.Back,
                    new Vector2(90.0f, 0.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_R_Calf", "Bip01_R_Thigh", ConstraintType.ConstraintHinge,
                    Vector3.Back, Vector3.Back,
                    new Vector2(90.0f, 0.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_Spine1", "Bip01_Pelvis", ConstraintType.ConstraintHinge,
                    Vector3.Forward, Vector3.Forward,
                    new Vector2(45.0f, 0.0f), new Vector2(-10.0f, 0.0f));
                CreateRagdollConstraint("Bip01_Head", "Bip01_Spine1", ConstraintType.ConstraintConetwist,
                    Vector3.Left, Vector3.Left,
                    new Vector2(0.0f, 30.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_L_UpperArm", "Bip01_Spine1", ConstraintType.ConstraintConetwist,
                    Vector3.Down, Vector3.Up,
                    new Vector2(45.0f, 45.0f), Vector2.ZERO, false);
                CreateRagdollConstraint("Bip01_R_UpperArm", "Bip01_Spine1", ConstraintType.ConstraintConetwist,
                    Vector3.Down, Vector3.Up,
                    new Vector2(45.0f, 45.0f), Vector2.ZERO, false);
                CreateRagdollConstraint("Bip01_L_Forearm", "Bip01_L_UpperArm", ConstraintType.ConstraintHinge,
                    Vector3.Back, Vector3.Back,
                    new Vector2(90.0f, 0.0f), Vector2.ZERO);
                CreateRagdollConstraint("Bip01_R_Forearm", "Bip01_R_UpperArm", ConstraintType.ConstraintHinge,
                    Vector3.Back, Vector3.Back,
                    new Vector2(90.0f, 0.0f), Vector2.ZERO);

                // Disable keyframe animation from all bones so that they will not interfere with the ragdoll
                var model = Node.GetComponent<AnimatedModel>();
                var skeleton = model.Skeleton;
                for (uint i = 0; i < skeleton.NumBones; ++i)
                    skeleton.GetBone(i).Animated = false;

                for (uint i = 0; i < skeleton.NumBones; ++i)
                {
                    var bone = skeleton.GetBone(i);
                    var boneNode = Node.GetChild(bone.NameHash, true);
                    var constraint = boneNode.GetComponent<Constraint>();
                    if (constraint != null)
                    {
                        var axis = (constraint.ConstraintType == ConstraintType.ConstraintHinge)
                            ? Vector3.Forward
                            : Vector3.Right;
                        
                        var a1 = constraint.Rotation * axis;
                        var otherBone = skeleton.Bones.FirstOrDefault(_ => _.Name == constraint.OtherBody.Node.Name);
                        if (otherBone != null)
                        {
                            var bindingPoseMatrix = bone.OffsetMatrix;
                            var poseMatrix = bindingPoseMatrix.Inverse();
                            var worldAxis = poseMatrix.Rotation * a1;
                            var otherAxis = otherBone.OffsetMatrix.Rotation* worldAxis;

                            var pos = bindingPoseMatrix * (otherBone.OffsetMatrix.Inverse() * constraint.OtherPosition);
                            var otherPos = otherBone.OffsetMatrix * (bindingPoseMatrix.Inverse() * constraint.Position);
                            Debug.WriteLine($"{bone.Name} to {otherBone.Name} {constraint.ConstraintType}:\n\t   my axis {SA(a1)},\n\tother axis {SA(otherAxis)}\n\tworld axis {SA(worldAxis)}");
                            Debug.WriteLine($"\t   my position {constraint.Position}, predicted {pos}, D = {(pos- constraint.Position).Length}");
                            Debug.WriteLine($"\tother position {constraint.OtherPosition}, predicted {otherPos}, D = {(otherPos - constraint.OtherPosition).Length}");

                            var r = constraint.Rotation;
                        }
                        else
                        {
                            Debug.WriteLine($"{bone.Name} -> {a1}");
                        }

                    }
                }
                
                // Finally remove self from the scene node. Note that this must be the last operation performed in the function
                Remove();
            }
        }

        private string SA(Vector3 v)
        {
            var maxT = 0.0f;
            string name = "?";
            foreach (var t in new []
            {
                Tuple.Create(nameof(Vector3.Forward), Vector3.Forward),
                Tuple.Create(nameof(Vector3.Back), Vector3.Back),
                Tuple.Create(nameof(Vector3.Right), Vector3.Right),
                Tuple.Create(nameof(Vector3.Left), Vector3.Left),
                Tuple.Create(nameof(Vector3.Up), Vector3.Up),
                Tuple.Create(nameof(Vector3.Down), Vector3.Down),
            })
            {
                var dotProduct = v.DotProduct(t.Item2);
                if (dotProduct > maxT)
                {
                    maxT = dotProduct;
                    name = t.Item1;
                }
            }

            return name;
        }

        private void CreateRagdollBone(string boneName, ShapeType type, Vector3 size, Vector3 position,
            Quaternion rotation)
        {
            // Find the correct child scene node recursively
            var boneNode = Node.GetChild(boneName, true);
            if (boneNode == null)
                //URHO3D_LOGWARNING("Could not find bone " + boneName + " for creating ragdoll physics components");
                return;

            var body = boneNode.CreateComponent<RigidBody>();
            // Set mass to make movable
            body.Mass = 1.0f;
            // Set damping parameters to smooth out the motion
            body.LinearDamping = 0.05f;
            body.AngularDamping = 0.85f;
            // Set rest thresholds to ensure the ragdoll rigid bodies come to rest to not consume CPU endlessly
            body.LinearRestThreshold = 1.5f;
            body.AngularRestThreshold = 2.5f;

            var shape = boneNode.CreateComponent<CollisionShape>();
            // We use either a box or a capsule shape for all of the bones
            if (type == ShapeType.ShapeBox)
                shape.SetBox(size, position, rotation);
            else
                shape.SetCapsule(size.X, size.Y, position, rotation);
        }

        private void CreateRagdollConstraint(string boneName, string parentName, ConstraintType type,
            Vector3 axis, Vector3 parentAxis, Vector2 highLimit, Vector2 lowLimit,
            bool disableCollision = true)
        {
            var boneNode = Node.GetChild(boneName, true);
            var parentNode = Node.GetChild(parentName, true);
            if (boneNode == null)
                //URHO3D_LOGWARNING("Could not find bone " + boneName + " for creating ragdoll constraint");
                return;
            if (parentNode == null)
                //URHO3D_LOGWARNING("Could not find bone " + parentName + " for creating ragdoll constraint");
                return;

            var constraint = boneNode.CreateComponent<Constraint>();
            constraint.ConstraintType = type;
            // Most of the constraints in the ragdoll will work better when the connected bodies don't collide against each other
            constraint.DisableCollision = disableCollision;
            // The connected body must be specified before setting the world position
            constraint.OtherBody = parentNode.GetComponent<RigidBody>();
            // Position the constraint at the child bone we are connecting
            constraint.WorldPosition = boneNode.WorldPosition;
            // Configure axes and limits
            constraint.SetAxis(axis);
            constraint.SetOtherAxis(parentAxis);
            constraint.HighLimit = highLimit;
            constraint.LowLimit = lowLimit;
        }
    }
}