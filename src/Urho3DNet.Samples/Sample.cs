using System;

namespace Urho3DNet.Samples
{
    public class Sample : Object
    {
        private Sprite logoSprite_;
        /// Scene.
        protected SharedPtr<Scene> scene_;
        /// Camera scene node.
        protected SharedPtr<Node> cameraNode_;
        /// Camera yaw angle.
        protected float yaw_;
        /// Camera pitch angle.
        protected float pitch_;
        /// Mouse mode option to use in the sample.
        protected MouseMode useMouseMode_;

        public Sample(Context context) : base(context)
        {
        }

        public virtual void Setup()
        {
        }

        public virtual void Start()
        {
            CreateLogo();
        }
        public virtual void Stop()
        {

        }

        protected void CreateLogo()
        {
            var logoTexture = Context.ResourceCache.GetResource<Texture2D>("Textures/rbfx-logo.png");
            if (logoTexture == null)
                return;

            logoSprite_ = Context.UI.Root.CreateChild<Sprite>();
            logoSprite_.Texture = (logoTexture);
            int textureWidth = logoTexture.Width;
            int textureHeight = logoTexture.Height;
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

            if (GetPlatform() != "Web")
            {
                if (useMouseMode_ == MouseMode.MmFree)
                    Context.Input.SetMouseVisible(true);

                var console = (Console)GetSubsystem("Console"); // TODO: is string literal appropiate? Can we get a generic?

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

        // If the user clicks the canvas, attempt to switch to relative mouse mode on web platform
        private void HandleMouseModeRequest(StringHash eventType, VariantMap eventData)
        {
            var console = (Console)GetSubsystem("Console"); // TODO: is string literal appropiate? Can we get a generic?
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
            bool mouseLocked = eventData["MouseLocked"].Bool;
            Context.Input.SetMouseVisible(!mouseLocked);
        }

        // TODO: Fix ProcessUtils
        protected string GetPlatform()
        {
            return "Windows";
        }

        // TODO: Fix MathDefs

        private Random random = new Random();
        protected float Random(float range)
        {
            return (float)random.NextDouble() * range;
        }
    }
}
