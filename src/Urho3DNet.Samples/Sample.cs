namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class Sample : Object
    {
        public const float TOUCH_SENSITIVITY = 2.0f;

        /// Camera yaw angle.
        protected float yaw_;

        /// Camera pitch angle.
        protected float pitch_;

        /// Flag to indicate whether touch input has been enabled.
        protected bool touchEnabled_;

        /// Mouse mode option to use in the sample.
        protected MouseMode useMouseMode_;

        /// Scene.
        private readonly SharedPtr<Scene> scene_ = new SharedPtr<Scene>(null);

        /// Camera scene node.
        private readonly SharedPtr<Node> cameraNode_ = new SharedPtr<Node>(null);

        /// Camera scene node.
        private readonly SharedPtr<Viewport> viewport_ = new SharedPtr<Viewport>(null);

        private readonly SharedPtr<Console> console_ = new SharedPtr<Console>(null);

        private readonly SharedPtr<DebugHud> debugHud_ = new SharedPtr<DebugHud>(null);

        private Sprite logoSprite_;


        public Console Console => console_.Value;

        public Sample(Context context) : base(context)
        {
        }

        protected Scene Scene
        {
            get => scene_?.Value;
            set => scene_.Value = value;
        }

        protected Node CameraNode
        {
            get => cameraNode_?.Value;
            set => cameraNode_.Value = value;
        }

        protected Viewport Viewport
        {
            get => viewport_?.Value;
            set => viewport_.Value = value;
        }

        public virtual void Setup()
        {
        }

        public virtual void Start()
        {
            CreateLogo();

            CreateConsoleAndDebugHud();

            // Subscribe key down event
            SubscribeToEvent(E.KeyDown, HandleKeyDown);
            // Subscribe key up event
            SubscribeToEvent(E.KeyUp, HandleKeyUp);
            // Subscribe scene update event
            SubscribeToEvent(E.SceneUpdate, HandleSceneUpdate);
        }

        protected void CreateConsoleAndDebugHud()
        {
            // Create console
            console_.Value = Context.GetSubsystem<Engine>().CreateConsole();

            // Create debug HUD.
            debugHud_.Value = Context.GetSubsystem<Engine>().CreateDebugHud();
        }

        public virtual void Stop()
        {
            cameraNode_.Dispose();
            scene_.Dispose();
            console_.Dispose();
            debugHud_.Dispose();
        }

        protected void CreateLogo()
        {
            var logoTexture = Context.ResourceCache.GetResource<Texture2D>("Textures/rbfx-logo.png");
            if (logoTexture == null)
                return;

            logoSprite_ = Context.UI.Root.CreateChild<Sprite>();
            logoSprite_.Texture = logoTexture;
            var textureWidth = logoTexture.Width;
            var textureHeight = logoTexture.Height;
            logoSprite_.Scale = new Vector2(256.0f / textureWidth, 256.0f / textureWidth);
            logoSprite_.Size = new IntVector2(textureWidth, textureHeight);
            logoSprite_.HotSpot = new IntVector2(textureWidth, textureHeight);
            logoSprite_.SetAlignment(HorizontalAlignment.HaRight, VerticalAlignment.VaBottom);
            logoSprite_.Opacity = 0.9f;
            logoSprite_.Priority = -100;
        }

        protected void InitMouseMode(MouseMode mode)
        {
            useMouseMode_ = mode;

            var input = Context.Input;

            if (ProcessUtils.Platform != "Web")
            {
                if (useMouseMode_ == MouseMode.MmFree)
                    Context.Input.SetMouseVisible(true);

                var console = GetSubsystem<Console>();

                if (useMouseMode_ != MouseMode.MmAbsolute)
                {
                    Context.Input.SetMouseMode(useMouseMode_);
                    if (console != null && console.IsVisible)
                        Context.Input.SetMouseMode(MouseMode.MmAbsolute, true);
                }
            }
            else
            {
                Context.Input.SetMouseVisible(true);
                SubscribeToEvent(E.MouseButtonDown, HandleMouseModeRequest);
                SubscribeToEvent(E.MouseModeChanged, HandleMouseModeChange);
            }
        }

        private void HandleSceneUpdate(VariantMap eventData)
        {
        }

        private void HandleKeyUp(VariantMap eventData)
        {
        }

        private void HandleKeyDown(VariantMap eventData)
        {
            var key = (Key) eventData[E.KeyDown.Key].Int;

            // Toggle console with F1 or backquote
            if (key == Key.KeyF1 || key == Key.KeyBackquote)
            {
#if URHO3D_RMLUI
                if (auto* ui = GetSubsystem<RmlUI>())
                {
                    if (ui->IsInputCaptured())
                        return;
                }
#endif
                var ui = GetSubsystem<UI>();
                if (ui != null)
                {
                    var element = ui.GetFocusElement();
                    if (element != null)
                        if (element.IsEditable)
                            return;
                }

                GetSubsystem<Console>()?.Toggle();
            }
            // Toggle debug HUD with F2
            else if (key == Key.KeyF2)
            {
                Context.Engine.CreateDebugHud().ToggleAll();
            }
        }

        // If the user clicks the canvas, attempt to switch to relative mouse mode on web platform
        private void HandleMouseModeRequest(StringHash eventType, VariantMap eventData)
        {
            var console = GetSubsystem<Console>();
            if (console != null && console.IsVisible)
                return;
            if (useMouseMode_ == MouseMode.MmAbsolute)
                Context.Input.SetMouseVisible(false);
            else if (useMouseMode_ == MouseMode.MmFree)
                Context.Input.SetMouseVisible(true);
            Context.Input.SetMouseMode(useMouseMode_);
        }

        private void HandleMouseModeChange(StringHash eventType, VariantMap eventData)
        {
            var mouseLocked = eventData[E.MouseModeChanged.MouseLocked].Bool;
            Context.Input.SetMouseVisible(!mouseLocked);
        }

        protected void CloseSample()
        {
            using (VariantMap args = new VariantMap())
            {
                args[E.KeyDown.Key] = (int)Key.KeyEscape;
                args[E.KeyDown.Scancode] = (int)Scancode.ScancodeEscape;
                args[E.KeyDown.Buttons] = 0;
                args[E.KeyDown.Qualifiers] = 0;
                args[E.KeyDown.Repeat] = false;
                SendEvent(E.KeyDown, args);
                
            }
            using (VariantMap args = new VariantMap())
            {
                args[E.KeyUp.Key] = (int)Key.KeyEscape;
                args[E.KeyUp.Scancode] = (int)Scancode.ScancodeEscape;
                args[E.KeyUp.Buttons] = 0;
                args[E.KeyUp.Qualifiers] = 0;
                SendEvent(E.KeyUp, args);
            }
        }

    }
}