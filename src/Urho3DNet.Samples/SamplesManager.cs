using System.Diagnostics;
using Urho3DNet;

namespace Urho3DNet.Samples
{
    public class SamplesManager : Application
    {
        private UIElement listViewHolder_;
        private Sprite logoSprite_;

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

            Context.Renderer.DefaultZone.FogColor = new Color(0.1f, 0.2f, 0.4f, 1.0f);

            Context.Input.SetMouseMode(MouseMode.MmFree);
            Context.Engine.CreateDebugHud().ToggleAll();
            
            ui.Root.SetDefaultStyle(resourceCache.GetResource<XMLFile>("UI/DefaultStyle.xml"));

            UIElement layout = ui.Root.CreateChild(nameof(UIElement));
            listViewHolder_ = layout;
            layout.LayoutMode = LayoutMode.LmVertical;
            layout.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            layout.Size = new IntVector2(300, 600);
            layout.SetStyleAuto();

            ListView list = (ListView)layout.CreateChild(nameof(ListView));
            list.MinSize = new IntVector2(300, 600);
            list.SelectOnClickEnd = true;
            list.HighlightMode = HighlightMode.HmAlways;
            list.SetStyleAuto();
            list.Name = "SampleList";

            // Get logo texture
            var logoTexture = resourceCache.GetResource<Texture2D>("Textures/rbfx-logo.png");
            if (logoTexture == null)
                return;

            logoSprite_ = (Sprite)ui.Root.CreateChild(nameof(Sprite));
            logoSprite_.Texture = (logoTexture);
            int textureWidth = logoTexture.Width;
            int textureHeight = logoTexture.Height;
            logoSprite_.Scale = new Vector2(256.0f / textureWidth, 256.0f / textureWidth);
            logoSprite_.Size = new IntVector2(textureWidth, textureHeight);
            logoSprite_.HotSpot = new IntVector2(textureWidth, textureHeight);
            logoSprite_.SetAlignment(HorizontalAlignment.HaRight,VerticalAlignment.VaBottom);
            logoSprite_.Opacity = 0.9f;
            logoSprite_.Priority = -100;


            base.Start();
        }

        public override void Stop()
        {
            base.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }
}
