using System.Diagnostics;
using Urho3DNet;

namespace Urho3DNet.Samples
{
    public class SamplesManager : Application
    {
        private UIElement listViewHolder_;
        private Sprite logoSprite_;
        private Sample runningSample_;

        public SamplesManager(Context context) : base(context)
        {
        }

        public override void Setup()
        {
            if (Debugger.IsAttached)
            {
                EngineParameters[Urho3D.EpFullScreen] = false;
                EngineParameters[Urho3D.EpWindowResizable] = true;
            }
            else
            {
                EngineParameters[Urho3D.EpFullScreen] = true;
            }
            EngineParameters[Urho3D.EpWindowTitle] = "SamplesManager";
            base.Setup();
        }

        public override void Start()
        {

            var ui = Context.UI;
            var resourceCache = Context.ResourceCache;

            SubscribeToEvent(E.Released, OnClickSample);
            SubscribeToEvent(E.KeyUp, OnKeyPress);

            // Register an object factory for our custom Rotator component so that we can create them to scene nodes
            //Context.RegisterFactory<Rotator>();

            Context.Input.SetMouseMode(MouseMode.MmFree);
            Context.Input.SetMouseVisible(true);

            Context.Engine.CreateDebugHud().ToggleAll();

            Context.Renderer.DefaultZone.FogColor = new Color(0.1f, 0.2f, 0.4f, 1.0f);

            Context.Input.SetMouseMode(MouseMode.MmFree);
            Context.Engine.CreateDebugHud().ToggleAll();
            
            ui.Root.SetDefaultStyle(resourceCache.GetResource<XMLFile>("UI/DefaultStyle.xml"));

            UIElement layout = ui.Root.CreateChild<UIElement>();
            listViewHolder_ = layout;
            layout.LayoutMode = LayoutMode.LmVertical;
            layout.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            layout.Size = new IntVector2(300, 600);
            layout.SetStyleAuto();

            ListView list = layout.CreateChild<ListView>();
            list.MinSize = new IntVector2(300, 600);
            list.SelectOnClickEnd = true;
            list.HighlightMode = HighlightMode.HmAlways;
            list.SetStyleAuto();
            list.Name = "SampleList";

            // Get logo texture
            var logoTexture = resourceCache.GetResource<Texture2D>("Textures/rbfx-logo.png");
            if (logoTexture == null)
                return;

            logoSprite_ = ui.Root.CreateChild<Sprite>();
            logoSprite_.Texture = (logoTexture);
            int textureWidth = logoTexture.Width;
            int textureHeight = logoTexture.Height;
            logoSprite_.Scale = new Vector2(256.0f / textureWidth, 256.0f / textureWidth);
            logoSprite_.Size = new IntVector2(textureWidth, textureHeight);
            logoSprite_.HotSpot = new IntVector2(textureWidth, textureHeight);
            logoSprite_.SetAlignment(HorizontalAlignment.HaRight,VerticalAlignment.VaBottom);
            logoSprite_.Opacity = 0.9f;
            logoSprite_.Priority = -100;

            RegisterSample<HelloWorld>();
            RegisterSample<AnimatingScene>();

            base.Start();
        }

        private void OnKeyPress(VariantMap obj)
        {
            
        }

        private void OnClickSample(VariantMap args)
        {
            StringHash sampleType = ((UIElement)args[E.Released.Element].Ptr).Vars["SampleType"].StringHash;
            if (sampleType.Hash == 0)
                return;

            StartSample(sampleType);
    }

        private void StartSample(StringHash sampleType)
        {
            UI ui = Context.UI;
            ui.Root.RemoveAllChildren();
            ui.SetFocusElement(null);

            runningSample_ = Context.CreateObject(sampleType) as Sample;
            if (runningSample_ != null)
                runningSample_.Start();
        }

        public override void Stop()
        {
            StopRunningSample();

            Context.Engine.DumpResources(true);
            base.Stop();
        }

        private void StopRunningSample()
        {
            if (runningSample_ != null)
            {
                runningSample_.Stop();
                runningSample_ = null;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        void RegisterSample<T>() where T:Urho3DNet.Object
        {
            Context.RegisterFactory<T>();

            var button = Context.CreateObject<Button>();
            button.MinHeight = 30;
            button.SetStyleAuto();
            button.SetVar("SampleType", new StringHash(typeof(T).Name));

            var title = button.CreateChild<Text>();
            title.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            title.SetText(typeof(T).Name);
            title.SetFont(Context.ResourceCache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 30);
            title.SetStyleAuto();

            var list = (ListView)Context.UI.Root.GetChild("SampleList", true);
            list.AddItem(button);
        }

    }
}
