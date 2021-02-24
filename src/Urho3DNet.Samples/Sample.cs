namespace Urho3DNet.Samples
{
    public class Sample : Object
    {
        private Sprite logoSprite_;

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

    }
}
