namespace Urho3DNet.Samples
{
    class Rotator : LogicComponent
    {
        public Vector3 RotationSpeed { get; set; }

        public Rotator(Context context) : base(context)
        {
            // Only the scene update event is needed: unsubscribe from the rest for optimization
            UpdateEventMask = UpdateEvent.UseUpdate;
        }

        public override void Update(float timeStep)
        {
            // Components have their scene node as a member variable for convenient access. Rotate the scene node now: construct a
            // rotation quaternion from Euler angles, scale rotation speed with the scene update time step
            Node.Rotate(new Quaternion(RotationSpeed.X * timeStep, RotationSpeed.Y * timeStep, RotationSpeed.Z * timeStep));
        }
    }
}
