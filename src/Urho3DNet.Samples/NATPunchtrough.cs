using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Text;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class NATPunchtrough : Sample
    {
        const int SERVER_PORT = 54654;
        private Text logHistoryText_;
        /// Log messages
        List<String> logHistory_ = new List<string>();

        private LineEdit natServerAddress_;
        private LineEdit natServerPort_;
        private Button saveNatSettingsButton_;
        private LineEdit guid_;
        private Button startServerButton_;
        private LineEdit serverGuid_;
        private Button connectButton_;


        public NATPunchtrough(Context context) : base(context)
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
            base.InitMouseMode(MouseMode.MmFree);
        }

        void CreateUI()
        {
            SetLogoVisible(true); // We need the full rendering window

            UIElement root = GetSubsystem<UI>().Root;
            var cache = GetSubsystem<ResourceCache>();
            var uiStyle = cache.GetResource<XMLFile>("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);

            var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");
            logHistoryText_ = root.CreateChild<Text>();
            logHistoryText_.SetFont(font, 12);
            logHistoryText_.Position = new IntVector2(20, -20);
            logHistoryText_.VerticalAlignment = (VerticalAlignment.VaBottom);
            while (logHistory_.Count < 20)
            {
                logHistory_.Add("");
            }

            // Create NAT server config fields
            int marginTop = 40;
            CreateLabel("1. Run NAT server somewhere, enter NAT server info and press 'Save NAT settings'", new IntVector2(20, marginTop - 20));
            natServerAddress_ = CreateLineEdit("127.0.0.1", 200, new IntVector2(20, marginTop));
            natServerPort_ = CreateLineEdit("61111", 100, new IntVector2(240, marginTop));
            saveNatSettingsButton_ = CreateButton("Save NAT settings", 160, new IntVector2(360, marginTop));

            // Create server start button
            marginTop = 120;
            CreateLabel("2. Create server and give others your server GUID", new IntVector2(20, marginTop - 20));
            guid_ = CreateLineEdit("Your server GUID", 200, new IntVector2(20, marginTop));
            startServerButton_ = CreateButton("Start server", 160, new IntVector2(240, marginTop));

            // Create client connection related fields
            marginTop = 200;
            CreateLabel("3. Input local or remote server GUID", new IntVector2(20, marginTop - 20));
            serverGuid_ = CreateLineEdit("Remote server GUID", 200, new IntVector2(20, marginTop));
            connectButton_ = CreateButton("Connect", 160, new IntVector2(240, marginTop));

            // No viewports or scene is defined. However, the default zone's fog color controls the fill color
            GetSubsystem<Renderer>().DefaultZone.FogColor = (new Color(0.0f, 0.0f, 0.1f));
        }

        void SubscribeToEvents()
        {
            SubscribeToEvent(E.ServerConnected, (HandleServerConnected));
            SubscribeToEvent(E.ServerDisconnected, (HandleServerDisconnected));
            SubscribeToEvent(E.ConnectFailed, (HandleConnectFailed));

            // NAT server connection related events
            SubscribeToEvent(E.NetworkNatMasterConnectionFailed, (HandleNatConnectionFailed));
            SubscribeToEvent(E.NetworkNatMasterConnectionSucceeded, (HandleNatConnectionSucceeded));
            SubscribeToEvent(E.NetworkNatMasterDisconnected, (HandleNatDisconnected));

            // NAT punchtrough request events
            SubscribeToEvent(E.NetworkNatPunchtroughSucceeded, (HandleNatPunchtroughSucceeded));
            SubscribeToEvent(E.NetworkNatPunchtroughFailed, (HandleNatPunchtroughFailed));

            SubscribeToEvent(E.ClientConnected, (HandleClientConnected));
            SubscribeToEvent(E.ClientDisconnected, (HandleClientDisconnected));

            saveNatSettingsButton_.SubscribeToEvent("Released", (HandleSaveNatSettings));
            startServerButton_.SubscribeToEvent( "Released", (HandleStartServer));
            connectButton_.SubscribeToEvent( "Released", HandleConnect);
        }

        Button CreateButton(String text, int width, IntVector2 position)
{
    var cache = GetSubsystem<ResourceCache>();
        var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");

        var button = GetSubsystem<UI>().Root.CreateChild<Button>();
        button.SetStyleAuto();
        button.SetFixedWidth(width);
        button.SetFixedHeight(30);
        button.Position = (position);

        var buttonText = button.CreateChild<Text>();
        buttonText.SetFont(font, 12);
        buttonText.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
        buttonText.SetText(text);

    return button;
}

    LineEdit CreateLineEdit(String placeholder, int width, IntVector2 pos)
    {
        var textEdit = GetSubsystem<UI>().Root.CreateChild<LineEdit>("");
        textEdit.SetStyleAuto();
        textEdit.SetFixedWidth(width);
        textEdit.SetFixedHeight(30);
        textEdit.Text = (placeholder);
        textEdit.Position = (pos);
        return textEdit;
    }

    void CreateLabel(String text, IntVector2 pos)
    {
        var cache = GetSubsystem<ResourceCache>();
        // Create log element to view latest logs from the system
        var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");
        var label = GetSubsystem<UI>().Root.CreateChild<Text>();
        label.SetFont(font, 12);
        label.SetColor(new Color(0.0f, 1.0f, 0.0f));
        label.Position = (pos);
        label.SetText(text);
    }

    void ShowLogMessage(String row)
    {
        logHistory_.RemoveAt(0);
        logHistory_.Add(row);

        // Concatenate all the rows in history
        StringBuilder allRows = new StringBuilder();
        for (int i = 0; i < logHistory_.Count; ++i)
            allRows.AppendLine(logHistory_[i]);

        logHistoryText_.SetText(allRows.ToString());
    }

    void HandleSaveNatSettings(StringHash eventType, VariantMap eventData)
    {
        // Save NAT server configuration
        GetSubsystem<Network>().SetNATServerInfo(natServerAddress_.Text, ushort.Parse(natServerPort_.Text, CultureInfo.InvariantCulture));
        ShowLogMessage("Saving NAT settings: " + natServerAddress_.Text + ":" + natServerPort_.Text);
    }

    void HandleServerConnected(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Client: Server connected!");
    }

    void HandleServerDisconnected(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Client: Server disconnected!");
    }

    void HandleConnectFailed(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Client: Connection failed!");
    }

    void HandleNatDisconnected(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Disconnected from NAT master server");
    }

    void HandleStartServer(StringHash eventType, VariantMap eventData)
    {
        GetSubsystem<Network>().StartServer(SERVER_PORT);
        ShowLogMessage("Server: Server started on port: " + SERVER_PORT);

        // Connect to the NAT server
        GetSubsystem<Network>().StartNATClient();
        ShowLogMessage("Server: Starting NAT client for server...");

        // Output our assigned GUID which others will use to connect to our server
        guid_.Text = (GetSubsystem<Network>().Guid);
        serverGuid_.Text = (GetSubsystem<Network>().Guid);
    }

    void HandleConnect(StringHash eventType, VariantMap eventData)
    {
        VariantMap userData = new VariantMap();
        userData["Name"] = "Urho3D";

        // Attempt connecting to server using custom GUID, Scene = null as a second parameter and user identity is passed as third parameter
        GetSubsystem<Network>().AttemptNATPunchtrough(serverGuid_.Text, null, userData);
        ShowLogMessage("Client: Attempting NAT punchtrough to guid: " + serverGuid_.Text);
    }

    void HandleNatConnectionFailed(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Connection to NAT master server failed!");
    }

    void HandleNatConnectionSucceeded(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Connection to NAT master server succeeded!");
    }

    void HandleNatPunchtroughSucceeded(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("NAT punchtrough succeeded!");
    }

    void HandleNatPunchtroughFailed(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("NAT punchtrough failed!");
    }

    void HandleClientConnected(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Server: Client connected!");
    }

    void HandleClientDisconnected(StringHash eventType, VariantMap eventData)
    {
        ShowLogMessage("Server: Client disconnected!");
    }

}
}