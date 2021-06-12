namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class LANDiscovery : Sample
    {
        private const int SERVER_PORT = 54654;

        private Button startServer_;
        private Button stopServer_;
        private Text serverList_;
        private Button refreshServerList_;

        public LANDiscovery(Context context) : base(context)
        {
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Enable OS cursor
            GetSubsystem<Input>().SetMouseVisible(true);

            // Create the user interface
            CreateUI();

            // Subscribe to UI and network events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmFree);
        }

        private void CreateUI()
        {
            //SetLogoVisible(true); // We need the full rendering window

            var graphics = GetSubsystem<Graphics>();
            var root = GetSubsystem<UI>().Root;
            var cache = GetSubsystem<ResourceCache>();
            var uiStyle = cache.GetResource<XMLFile>("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);

            var marginTop = 20;
            CreateLabel("1. Start server", new IntVector2(20, marginTop - 20));
            startServer_ = CreateButton("Start server", 160, new IntVector2(20, marginTop));
            stopServer_ = CreateButton("Stop server", 160, new IntVector2(20, marginTop));
            stopServer_.IsVisible = false;

            // Create client connection related fields
            marginTop += 80;
            CreateLabel("2. Discover LAN servers", new IntVector2(20, marginTop - 20));
            refreshServerList_ = CreateButton("Search...", 160, new IntVector2(20, marginTop));

            marginTop += 80;
            CreateLabel("Local servers:", new IntVector2(20, marginTop - 20));
            serverList_ = CreateLabel("", new IntVector2(20, marginTop));

            // No viewports or scene is defined. However, the default zone's fog color controls the fill color
            GetSubsystem<Renderer>().DefaultZone.FogColor = new Color(0.0f, 0.0f, 0.1f);
        }

        private void SubscribeToEvents()
        {
            SubscribeToEvent(E.NetworkHostDiscovered, HandleNetworkHostDiscovered);

            startServer_.SubscribeToEvent("Released", HandleStartServer);
            stopServer_.SubscribeToEvent("Released", HandleStopServer);
            refreshServerList_.SubscribeToEvent("Released", HandleDoNetworkDiscovery);
        }

        private Button CreateButton(string text, int width, IntVector2 position)
        {
            var cache = GetSubsystem<ResourceCache>();
            var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");

            var button = GetSubsystem<UI>().Root.CreateChild<Button>();
            button.SetStyleAuto();
            button.SetFixedWidth(width);
            button.SetFixedHeight(30);
            button.Position = position;

            var buttonText = button.CreateChild<Text>();
            buttonText.SetFont(font, 12);
            buttonText.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
            buttonText.SetText(text);

            return button;
        }

        private Text CreateLabel(string text, IntVector2 pos)
        {
            var cache = GetSubsystem<ResourceCache>();
            // Create log element to view latest logs from the system
            var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");
            var label = GetSubsystem<UI>().Root.CreateChild<Text>();
            label.SetFont(font, 12);
            label.SetColor(new Color(0.0f, 1.0f, 0.0f));
            label.Position = pos;
            label.SetText(text);
            return label;
        }

        private void HandleNetworkHostDiscovered(StringHash eventType, VariantMap eventData)
        {
            var text = serverList_.GetText();
            var data = eventData[E.NetworkHostDiscovered.Beacon].VariantMap;
            text += "\n" + data["Name"].String + "(" + data["Players"].Int + ")" +
                    eventData[E.NetworkHostDiscovered.Address].String + ":" +
                    eventData[E.NetworkHostDiscovered.Port].Int;
            serverList_.SetText(text);
        }

        private void HandleStartServer(StringHash eventType, VariantMap eventData)
        {
            if (GetSubsystem<Network>().StartServer(SERVER_PORT))
            {
                var data = new VariantMap();
                data["Name"] = "Test server";
                data["Players"] = 100;
                /// Set data which will be sent to all who requests LAN network discovery
                GetSubsystem<Network>().SetDiscoveryBeacon(data);
                startServer_.IsVisible = false;
                stopServer_.IsVisible = true;
            }
        }

        private void HandleStopServer(StringHash eventType, VariantMap eventData)
        {
            GetSubsystem<Network>().StopServer();
            startServer_.IsVisible = true;
            stopServer_.IsVisible = false;
        }

        private void HandleDoNetworkDiscovery(StringHash eventType, VariantMap eventData)
        {
            /// Pass in the port that should be checked
            GetSubsystem<Network>().DiscoverHosts(SERVER_PORT);
            serverList_.SetText("");
        }
    }
}