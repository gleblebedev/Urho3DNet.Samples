using System;
using System.Collections.Generic;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    internal class ActionsSample : Sample
    {
        private List<Node> _targets = new List<Node>();
        private readonly SharedPtr<UIElement> listViewHolder_ = new SharedPtr<UIElement>(null);
        private Node _boxNode;
        private ActionManager _actionManager;

        public ActionsSample(Context context) : base(context)
        {
            _actionManager = new ActionManager(context);
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Create the scene content
            CreateScene();

            // Create the UI content
            CreateInstructions();

            // Setup the viewport for displaying the scene
            SetupViewport();

            // Hook up to the frame update events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmFree);

            SubscribeToEvent(E.Released, OnClick);
        }

        private void OnClick(VariantMap eventData)
        {
            var sampleType = ((UIElement)eventData[E.Released.Element].Ptr).Vars["TargetNodeIndex"].Int;
            var target = _targets[sampleType];

            var sideFromCube = new Matrix3x4(1, 0, 0, 1, 0, 1, 0, 0, 0, 0, 1, 0);
            var targetContact = target.WorldTransform* sideFromCube;
            var nodeContact = _boxNode.WorldTransform * sideFromCube;
            //var contactDiff = targetContact * nodeContact.Inverse();
            //var nodeDiff = contactDiff * sideFromCube.Inverse();

            var contactDiff = nodeContact.Inverse() * targetContact;
            var nodeDiff = contactDiff;

            //var contactDiff = nodeContact.Inverse() * targetContact;
            //var nodeDiff = sideFromCube.Inverse() * contactDiff;

            nodeDiff.Decompose(out var translation, out var rotation, out var scale);
            var angles = rotation.EulerAngles;
            _actionManager.AddAction(
            new ActionBuilder(Context)
                .MoveBy(2.0f, translation)
                .Also(new ActionBuilder(Context).RotateBy(2.0f, new Quaternion(angles)).Build())
                .ElasticOut()
                .Build(), target);
        }

        public override void Stop()
        {
            UnsubscribeFromEvent(E.Update);
            base.Stop();
        }

        private void CreateScene()
        {
            Scene = new Scene(Context);

            // Create the Octree component to the scene so that drawable objects can be rendered. Use default volume
            // (-1000, -1000, -1000) to (1000, 1000, 1000)
            Scene.CreateComponent<Octree>();

            // Create a Zone component into a child scene node. The Zone controls ambient lighting and fog settings. Like the Octree,
            // it also defines its volume with a bounding box, but can be rotated (so it does not need to be aligned to the world X, Y
            // and Z axes.) Drawable objects "pick up" the zone they belong to and use it when rendering; several zones can exist
            var zoneNode = Scene.CreateChild("Zone");
            var zone = zoneNode.CreateComponent<Zone>();
            // Set same volume as the Octree, set a close bluish fog and some ambient light
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = new Color(0.05f, 0.1f, 0.15f);
            zone.FogColor = new Color(0.1f, 0.2f, 0.3f);
            zone.FogStart = 10.0f;
            zone.FogEnd = 100.0f;

            // Create randomly positioned and oriented box StaticModels in the scene
            _boxNode = Scene.CreateChild();
            _boxNode.Position = new Vector3(0,0,4);
            // Orient using random pitch, yaw and roll Euler angles
            _boxNode.Rotation = new Quaternion(MathDefs.Random(360.0f), MathDefs.Random(360.0f),
                MathDefs.Random(360.0f));
            var boxObject = _boxNode.CreateComponent<StaticModel>();
            boxObject.SetModel(Context.ResourceCache.GetResource<Model>("Models/Box.mdl"));
            boxObject.SetMaterial(Context.ResourceCache.GetResource<Material>("Materials/Stone.xml"));

            // Create the camera. Let the starting position be at the world origin. As the fog limits maximum visible distance, we can
            // bring the far clip plane closer for more effective culling of distant objects
            CameraNode = Scene.CreateChild("Camera");
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = 100.0f;

            // Create a point light to the camera scene node
            var light = CameraNode.CreateComponent<Light>();
            light.LightType = LightType.LightPoint;
            light.Range = 30.0f;
        }
        
        private void CreateInstructions()
        {
            var ui = Context.UI;
            var resourceCache = Context.ResourceCache;
            ui.Root.SetDefaultStyle(resourceCache.GetResource<XMLFile>("UI/DefaultStyle.xml"));

            var layout = ui.Root.CreateChild<UIElement>();
            listViewHolder_.Value = layout;
            layout.LayoutMode = LayoutMode.LmVertical;
            layout.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            layout.Size = new IntVector2(300, 600);
            layout.SetStyleAuto();

            var list = layout.CreateChild<ListView>();
            list.MinSize = new IntVector2(300, 300);
            list.SelectOnClickEnd = true;
            list.HighlightMode = HighlightMode.HmAlways;
            list.SetStyleAuto();
            list.Name = "SampleList";

            const int NumTargets = 6;
            for (int i = 0; i < NumTargets; i++)
            {
                var button = Context.CreateObject<Button>();
                button.MinHeight = 30;
                button.SetStyleAuto();
                button.SetVar("TargetNodeIndex", i);

                var title = button.CreateChild<Text>();
                title.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
                title.SetText($"Target {i+1}");
                title.SetFont(Context.ResourceCache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 30);
                title.SetStyleAuto();

                list.AddItem(button);

                var target = Scene.CreateChild();
                var a = i * Math.PI * 2.0 / NumTargets;
                target.Position = new Vector3((float)Math.Cos(a), (float)Math.Sin(a), 2.75f)*6;
                // Orient using random pitch, yaw and roll Euler angles
                target.Rotation = new Quaternion(MathDefs.Random(360.0f), MathDefs.Random(360.0f),
                    MathDefs.Random(360.0f));
                var boxObject = target.CreateComponent<StaticModel>();
                boxObject.SetModel(Context.ResourceCache.GetResource<Model>("Models/Box.mdl"));
                boxObject.SetMaterial(Context.ResourceCache.GetResource<Material>("Materials/Stone.xml"));

                
                _targets.Add(target);
            }
        }

        private void SetupViewport()
        {
            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
            var viewport = new Viewport(Context, Scene, CameraNode.GetComponent<Camera>());
            Context.Renderer.SetViewport(0, viewport);
        }

        private void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);
        }


        private void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            // Take the frame time step, which is stored as a float
            var timeStep = eventData["TimeStep"].Float;
            _actionManager.Update(timeStep);
                
        }
    }
}