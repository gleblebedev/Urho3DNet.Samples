﻿using System;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class Sample : Object
    {
        public const float TOUCH_SENSITIVITY = 2.0f;

        private Sprite logoSprite_;
        /// Scene.
        private SharedPtr<Scene> scene_;
        protected Scene Scene { get { return scene_?.Value; } set { scene_ = new SharedPtr<Scene>(value); } }
        /// Camera scene node.
        private SharedPtr<Node> cameraNode_;
        protected Node CameraNode { get { return cameraNode_?.Value; } set { cameraNode_ = new SharedPtr<Node>(value); } }
        /// Camera yaw angle.
        protected float yaw_;
        /// Camera pitch angle.
        protected float pitch_;
        /// Flag to indicate whether touch input has been enabled.
        protected bool touchEnabled_;
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

            // Subscribe key down event
            SubscribeToEvent(E.KeyDown, HandleKeyDown);
            // Subscribe key up event
            SubscribeToEvent(E.KeyUp, HandleKeyUp);
            // Subscribe scene update event
            SubscribeToEvent(E.SceneUpdate, HandleSceneUpdate);

        }

        private void HandleSceneUpdate(VariantMap eventData)
        {
            
        }

        private void HandleKeyUp(VariantMap eventData)
        {
            
        }

        private void HandleKeyDown(VariantMap eventData)
        {
            Key key = (Key)eventData[E.KeyDown.Key].Int;

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
                    var element = ui.FocusElement;
                    if (element != null)
                    {
                        if (element.IsEditable)
                            return;
                    }
                }
                GetSubsystem<Console>()?.Toggle();
                return;
            }
            // Toggle debug HUD with F2
            else if (key == Key.KeyF2)
            {
                Context.Engine.CreateDebugHud().ToggleAll();
                return;
            }
        }

        public virtual void Stop()
        {
            cameraNode_.Dispose();
            scene_.Dispose();
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
            bool mouseLocked = eventData[E.MouseModeChanged.MouseLocked].Bool;
            Context.Input.SetMouseVisible(!mouseLocked);
        }
    }
}
