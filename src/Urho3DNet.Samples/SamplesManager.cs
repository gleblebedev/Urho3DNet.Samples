using System;
using System.Diagnostics;

namespace Urho3DNet.Samples
{
    public class SamplesManager : Application
    {
        private readonly SharedPtr<UIElement> listViewHolder_ = new SharedPtr<UIElement>(null);
        private readonly SharedPtr<Sprite> logoSprite_ = new SharedPtr<Sprite>(null);
        private readonly SharedPtr<Sample> runningSample_ = new SharedPtr<Sample>(null);
        private bool isClosing_;

        public SamplesManager(Context context) : base(context)
        {
        }

        public override void Setup()
        {
            if (Debugger.IsAttached)
            {
                EngineParameters[Urho3D.EpFullScreen] = false;
                EngineParameters[Urho3D.EpWindowResizable] = true;
                //EngineParameters[Urho3D.EpGpuDebug] = true;
                //EngineParameters[Urho3D.EpOpenXR] = true;
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
            SubscribeToEvent(E.BeginFrame, OnFrameStart);
            SubscribeToEvent(E.LogMessage, OnLogMessage);

            // Register an object factory for our custom Rotator component so that we can create them to scene nodes
            //Context.RegisterFactory<Rotator>();

            Context.Input.SetMouseMode(MouseMode.MmFree);
            Context.Input.SetMouseVisible(true);

            Context.Engine.CreateDebugHud().ToggleAll();

            Context.Renderer.DefaultZone.FogColor = new Color(0.1f, 0.2f, 0.4f, 1.0f);

            Context.Input.SetMouseMode(MouseMode.MmFree);
            Context.Engine.CreateDebugHud().ToggleAll();

            ui.Root.SetDefaultStyle(resourceCache.GetResource<XMLFile>("UI/DefaultStyle.xml"));

            var layout = ui.Root.CreateChild<UIElement>();
            listViewHolder_.Value = layout;
            layout.LayoutMode = LayoutMode.LmVertical;
            layout.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            layout.Size = new IntVector2(300, 600);
            layout.SetStyleAuto();

            var list = layout.CreateChild<ListView>();
            list.MinSize = new IntVector2(300, 600);
            list.SelectOnClickEnd = true;
            list.HighlightMode = HighlightMode.HmAlways;
            list.SetStyleAuto();
            list.Name = "SampleList";

            // Get logo texture
            var logoTexture = resourceCache.GetResource<Texture2D>("Textures/rbfx-logo.png");
            if (logoTexture == null)
                return;

            var logoSprite = ui.Root.CreateChild<Sprite>();
            logoSprite_.Value = logoSprite;
            logoSprite.Texture = logoTexture;
            var textureWidth = logoTexture.Width;
            var textureHeight = logoTexture.Height;
            logoSprite.Scale = new Vector2(256.0f / textureWidth, 256.0f / textureWidth);
            logoSprite.Size = new IntVector2(textureWidth, textureHeight);
            logoSprite.HotSpot = new IntVector2(textureWidth, textureHeight);
            logoSprite.SetAlignment(HorizontalAlignment.HaRight, VerticalAlignment.VaBottom);
            logoSprite.Opacity = 0.9f;
            logoSprite.Priority = -100;

            RegisterSample<HelloWorld>(); //01
            RegisterSample<AnimatingScene>(); //05
            RegisterSample<NavigationDemo>(); //15
            RegisterSample<SceneReplication>(); //17
            RegisterSample<ConsoleInput>();
            RegisterSample<ActionsSample>();
            Context.RegisterFactory<KinematicCharacter>();
            RegisterSample<KinematicCharacterDemo>();
            RegisterSample<CharacterDemo>();
            RegisterSample<Ragdolls>();
            RegisterSample<InverseKinematics>();
            RegisterSample<RaycastVehicleDemo>();
            RegisterSample<DynamicGeometry>();
            RegisterSample<NATPunchtrough>(); //52
            RegisterSample<LANDiscovery>(); //53
            RegisterSample<ConfigSample>();
            Context.RegisterFactory<SampleConfigFile>();

            base.Start();
        }

        private void OnLogMessage(VariantMap obj)
        {
            var level = (LogLevel)obj[E.LogMessage.Level].Int;
            var message = obj[E.LogMessage.Message].String;
            switch (level)
            {
                case LogLevel.LogError:
#if DEBUG
                if (!message.Contains("Failed to register"))
                    throw new ApplicationException(message);
#endif
                    Trace.WriteLine(message);
                    break;
                default:
                    Trace.WriteLine(message);
                    break;
            }
        }

        public override void Stop()
        {
            StopRunningSample();

            Context.Engine.DumpResources(true);
            base.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        private void OnFrameStart(VariantMap obj)
        {
            if (isClosing_)
            {
                isClosing_ = false;
                if (runningSample_ != null)
                {
                    var input = Context.Input;
                    var ui = Context.UI;

                    StopRunningSample();

                    input.SetMouseMode(MouseMode.MmFree);
                    input.SetMouseVisible(true);
                    ui.Cursor = null;
                    ui.Root.RemoveAllChildren();
                    ui.Root.AddChild(listViewHolder_);
                    ui.Root.AddChild(logoSprite_);
                }
                else
                {
                    var console = GetSubsystem<Console>();
                    if (console != null)
                        if (console.IsVisible)
                        {
                            console.IsVisible = false;
                            return;
                        }

                    Context.Engine.Exit();
                }
            }
        }

        private void OnKeyPress(VariantMap eventData)
        {
            var key = (Key) eventData[E.KeyDown.Key].Int;

            if (key == Key.KeyEscape)
                isClosing_ = true;
        }

        private void OnClickSample(VariantMap eventData)
        {
            if (runningSample_.Value == null)
            {
                var sampleType = ((UIElement) eventData[E.Released.Element].Ptr).Vars["SampleType"].StringHash;
                if (sampleType.Hash == 0)
                    return;

                StartSample(sampleType);
            }
        }

        private void StartSample(StringHash sampleType)
        {
            var ui = Context.UI;
            ui.Root.RemoveAllChildren();
            ui.SetFocusElement(null);

            runningSample_.Value = Context.CreateObject(sampleType) as Sample;
            runningSample_.Value?.Start();
        }

        private void StopRunningSample()
        {
            Sample sample = runningSample_;
            if (sample != null)
            {
                sample.Stop();
                runningSample_.Dispose();
            }
        }

        private void RegisterSample<T>() where T : Object
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

            var list = (ListView) Context.UI.Root.GetChild("SampleList", true);
            list.AddItem(button);
        }
    }
}