namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class HelloWorld : Sample
    {
        public HelloWorld(Context context) : base(context)
        {
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Create "Hello World" Text
            CreateText();

            // Finally subscribe to the update event. Note that by subscribing events at this point we have already missed some events
            // like the ScreenMode event sent by the Graphics subsystem when opening the application window. To catch those as well we
            // could subscribe in the constructor instead.
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmFree);

        }

        void CreateText()
        {
            var cache = Context.ResourceCache;

            
            // Construct new Text object
            Text helloText = Context.CreateObject<Text>();

            // Set String to display
            helloText.SetText("Hello World from Urho3D!");

            // Set font and text color
            helloText.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 30);
            helloText.SetColor(new Color(0.0f, 1.0f, 0.0f));

            // Align Text center-screen
            helloText.HorizontalAlignment = HorizontalAlignment.HaCenter;
            helloText.VerticalAlignment = VerticalAlignment.VaCenter;

            // Add Text instance to the UI root element
            GetSubsystem<UI>().Root.AddChild(helloText);
        }

        void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);
        }

        void HandleUpdate(VariantMap eventData)
        {
            // Do nothing for now, could be extended to eg. animate the display
        }

    }
}