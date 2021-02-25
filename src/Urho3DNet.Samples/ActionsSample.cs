namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    class ActionsSample: Sample
    {
        public ActionsSample(Context context) : base(context)
        {
        }
        
        void CreateScene()
        {
            Scene = new Scene(Context);

            // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
            // (-1000, -1000, -1000) to (1000, 1000, 1000)
            Scene.CreateComponent<Octree>();

            // Create the camera. Let the starting position be at the world origin. As the fog limits maximum visible distance, we can
            // bring the far clip plane closer for more effective culling of distant objects
            CameraNode = Scene.CreateChild("Camera");
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 100.0f;
        }

    }
}