using System;
using System.Collections.Generic;
using System.Numerics;

namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    public class SceneReplication : Sample
    {
        private Text instructionsText_;
        private Text packetsIn_;
        private Text packetsOut_;
        private UIElement buttonContainer_;
        private LineEdit textEdit_;
        private Button connectButton_;
        private Button disconnectButton_;
        private Button startServerButton_;
        /// Packet counter UI update timer
        Timer packetCounterTimer_ = new Timer();
        uint clientObjectID_;

        /// Mapping from client connections to controllable objects.
        Dictionary<Connection, Node> serverObjects_ = new Dictionary<Connection, Node>();

        // UDP port we will use
        static ushort SERVER_PORT = 2345;
        // Identifier for our custom remote event we use to tell the client which object they control
        static StringHash E_CLIENTOBJECTID = new StringHash("ClientObjectID");
        // Identifier for the node ID parameter in the event data
        static StringHash P_ID = new StringHash("ID");

        // Control bits we define
        const uint CTRL_FORWARD = 1;
        const uint CTRL_BACK = 2;
        const uint CTRL_LEFT = 4;
        const uint CTRL_RIGHT = 8;


        public SceneReplication(Context context) : base(context)
        {
        }
        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Create the scene content
            CreateScene();

            // Create the UI content
            CreateUI();

            // Setup the viewport for displaying the scene
            SetupViewport();

            // Hook up to necessary events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            base.InitMouseMode(MouseMode.MmRelative);
        }

        void CreateScene()
        {
            Scene = new Scene(Context);

            var cache = GetSubsystem<ResourceCache>();

            // Create octree and physics world with default settings. Create them as local so that they are not needlessly replicated
            // when a client connects
            Scene.CreateComponent<Octree>(CreateMode.Local);
            Scene.CreateComponent<PhysicsWorld>(CreateMode.Local);

            // All static scene content and the camera are also created as local, so that they are unaffected by scene replication and are
            // not removed from the client upon connection. Create a Zone component first for ambient lighting & fog control.
            Node zoneNode = Scene.CreateChild("Zone", CreateMode.Local);
            var zone = zoneNode.CreateComponent<Zone>();
            zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
            zone.AmbientColor = (new Color(0.1f, 0.1f, 0.1f));
            zone.FogStart = (100.0f);
            zone.FogEnd = (300.0f);

            // Create a directional light without shadows
            Node lightNode = Scene.CreateChild("DirectionalLight", CreateMode.Local);
            lightNode.Direction = (new Vector3(0.5f, -1.0f, 0.5f));
            var light = lightNode.CreateComponent<Light>();
            light.LightType = LightType.LightDirectional;
            light.Color = (new Color(0.2f, 0.2f, 0.2f));
            light.SpecularIntensity  = (1.0f);

            // Create a "floor" consisting of several tiles. Make the tiles physical but leave small cracks between them
            for (int y = -20; y <= 20; ++y)
            {
                for (int x = -20; x <= 20; ++x)
                {
                    Node floorNode = Scene.CreateChild("FloorTile", CreateMode.Local);
                    floorNode.Position = (new Vector3(x * 20.2f, -0.5f, y * 20.2f));
                    floorNode.SetScale(new Vector3(20.0f, 1.0f, 20.0f));
                    var floorObject = floorNode.CreateComponent<StaticModel>();
                    floorObject.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
                    floorObject.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));

                    var body = floorNode.CreateComponent<RigidBody>();
                    body.Friction = (1.0f);
                    var shape = floorNode.CreateComponent<CollisionShape>();
                    shape.SetBox(Vector3.One);
                }
            }

            // Create the camera. Limit far clip distance to match the fog
            // The camera needs to be created into a local node so that each client can retain its own camera, that is unaffected by
            // network messages. Furthermore, because the client removes all replicated scene nodes when connecting to a server scene,
            // the screen would become blank if the camera node was replicated (as only the locally created camera is assigned to a
            // viewport in SetupViewports() below)
            CameraNode = Scene.CreateChild("Camera", CreateMode.Local);
            var camera = CameraNode.CreateComponent<Camera>();
            camera.FarClip = (300.0f);

            // Set an initial position for the camera scene node above the plane
            CameraNode.Position = (new Vector3(0.0f, 5.0f, 0.0f));
        }

        void CreateUI()
        {
            var cache = GetSubsystem<ResourceCache>();
            var ui = GetSubsystem<UI>();
            UIElement root = ui.Root;
            var uiStyle = cache.GetResource<XMLFile>("UI/DefaultStyle.xml");
            // Set style to the UI root so that elements will inherit it
            root.SetDefaultStyle(uiStyle);

            // Create a Cursor UI element because we want to be able to hide and show it at will. When hidden, the mouse cursor will
            // control the camera, and when visible, it can interact with the login UI
            Cursor cursor = (new Cursor(Context));
            cursor.SetStyleAuto(uiStyle);
            ui.Cursor = (cursor);
            // Set starting position of the cursor at the rendering window center
            var graphics = GetSubsystem<Graphics>();
            cursor.Position = new IntVector2(graphics.Width / 2, graphics.Height / 2);

            // Construct the instructions text element
            instructionsText_ = ui.Root.CreateChild<Text>();
            instructionsText_.SetText(
                "Use WASD keys to move and RMB to rotate view"
            );
            instructionsText_.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            // Position the text relative to the screen center
            instructionsText_.HorizontalAlignment = HorizontalAlignment.HaCenter;
            instructionsText_.VerticalAlignment = VerticalAlignment.VaCenter;
            instructionsText_.Position = new IntVector2(0, graphics.Height / 4);
            // Hide until connected
            instructionsText_.IsVisible = (false);

            packetsIn_ = ui.Root.CreateChild<Text>();
            packetsIn_.SetText("Packets in : 0");
            packetsIn_.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            packetsIn_.HorizontalAlignment = HorizontalAlignment.HaLeft;
            packetsIn_.VerticalAlignment = VerticalAlignment.VaCenter;
            packetsIn_.Position = new IntVector2(10, -10);

            packetsOut_ = ui.Root.CreateChild<Text>();
            packetsOut_.SetText("Packets out: 0");
            packetsOut_.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
            packetsOut_.HorizontalAlignment = HorizontalAlignment.HaLeft;
            packetsOut_.VerticalAlignment = VerticalAlignment.VaCenter;
            packetsOut_.Position = new IntVector2(10, 10);

            buttonContainer_ = root.CreateChild<UIElement>();
            buttonContainer_.SetFixedSize(500, 20);
            buttonContainer_.Position = new IntVector2(20, 20);
            buttonContainer_.LayoutMode = LayoutMode.LmHorizontal;

            textEdit_ = buttonContainer_.CreateChild<LineEdit>();
            textEdit_.SetStyleAuto();

            connectButton_ = CreateButton("Connect", 90);
            disconnectButton_ = CreateButton("Disconnect", 100);
            startServerButton_ = CreateButton("Start Server", 110);

            UpdateButtons();
        }

        void SetupViewport()
        {
            var renderer = GetSubsystem<Renderer>();

            // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
            Viewport viewport = (new Viewport(Context, Scene, CameraNode.GetComponent<Camera>()));
            renderer.SetViewport(0, viewport);
        }

        void SubscribeToEvents()
        {
            // Subscribe to fixed timestep physics updates for setting or applying controls
            SubscribeToEvent(E.PhysicsPreStep, (HandlePhysicsPreStep));

            // Subscribe HandlePostUpdate() method for processing update events. Subscribe to PostUpdate instead
            // of the usual Update so that physics simulation has already proceeded for the frame, and can
            // accurately follow the object with the camera
            SubscribeToEvent(E.PostUpdate, (HandlePostUpdate));

            // Subscribe to button actions
            connectButton_.SubscribeToEvent( E.Released, (HandleConnect));
            disconnectButton_.SubscribeToEvent(E.Released, (HandleDisconnect));
            startServerButton_.SubscribeToEvent( E.Released, (HandleStartServer));

            // Subscribe to network events
            SubscribeToEvent(E.ServerConnected, (HandleConnectionStatus));
            SubscribeToEvent(E.ServerDisconnected, (HandleConnectionStatus));
            SubscribeToEvent(E.ConnectFailed, (HandleConnectionStatus));
            SubscribeToEvent(E.ClientConnected, (HandleClientConnected));
            SubscribeToEvent(E.ClientDisconnected, (HandleClientDisconnected));
            // This is a custom event, sent from the server to the client. It tells the node ID of the object the client should control
            SubscribeToEvent(E_CLIENTOBJECTID, (HandleClientObjectID));
            // Events sent between client & server (remote events) must be explicitly registered or else they are not allowed to be received
            GetSubsystem<Network>().RegisterRemoteEvent(E_CLIENTOBJECTID);
        }

        Button CreateButton(String text, int width)
{
    var cache = GetSubsystem<ResourceCache>();
        var font = cache.GetResource<Font>("Fonts/Anonymous Pro.ttf");

        var button = buttonContainer_.CreateChild<Button>();
        button.SetStyleAuto();
        button.SetFixedWidth(width);

        var buttonText = button.CreateChild<Text>();
        buttonText.SetFont(font, 12);
        buttonText.SetAlignment(HorizontalAlignment.HaCenter, VerticalAlignment.VaCenter);
        buttonText.SetText(text);

    return button;
}

    void UpdateButtons()
    {
        var network = GetSubsystem<Network>();
        Connection serverConnection = network.ServerConnection;
        bool serverRunning = network.IsServerRunning;

        // Show and hide buttons so that eg. Connect and Disconnect are never shown at the same time
        connectButton_.IsVisible = (serverConnection == null && !serverRunning);
        disconnectButton_.IsVisible = (serverConnection != null || serverRunning);
        startServerButton_.IsVisible = (serverConnection == null && !serverRunning);
        textEdit_.IsVisible = (serverConnection == null && !serverRunning);
    }

    Node CreateControllableObject()
    {
        var cache = GetSubsystem<ResourceCache>();

        // Create the scene node & visual representation. This will be a replicated object
        Node ballNode = Scene.CreateChild("Ball");
        ballNode.Position = (new Vector3(MathDefs.Random(40.0f) - 20.0f, 5.0f, MathDefs.Random(40.0f) - 20.0f));
        ballNode.SetScale(0.5f);
        var ballObject = ballNode.CreateComponent<StaticModel>();
        ballObject.SetModel(cache.GetResource<Model>("Models/Sphere.mdl"));
        ballObject.SetMaterial(cache.GetResource<Material>("Materials/StoneSmall.xml"));

        // Create the physics components
        var body = ballNode.CreateComponent<RigidBody>();
        body.Mass = (1.0f);
        body.Friction = (1.0f);
        // In addition to friction, use motion damping so that the ball can not accelerate limitlessly
        body.LinearDamping = (0.5f);
        body.AngularDamping = (0.5f);
        var shape = ballNode.CreateComponent<CollisionShape>();
        shape.SetSphere(1.0f);

        // Create a random colored point light at the ball so that can see better where is going
        var light = ballNode.CreateComponent<Light>();
        light.Range = (3.0f);
        light.Color = new Color(0.5f + ((uint)MathDefs.Rand() & 1u) * 0.5f, 0.5f + ((uint)MathDefs.Rand() & 1u) * 0.5f, 0.5f + ((uint)MathDefs.Rand() & 1u) * 0.5f);

        return ballNode;
    }

    void MoveCamera()
    {
        // Right mouse button controls mouse cursor visibility: hide when pressed
        var ui = GetSubsystem<UI>();
        var input = GetSubsystem<Input>();
        ui.Cursor.IsVisible = (!input.GetMouseButtonDown(MouseButton.MousebRight));

        // Mouse sensitivity as degrees per pixel
        const float MOUSE_SENSITIVITY = 0.1f;

        // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch and only move the camera
        // when the cursor is hidden
        if (!ui.Cursor.IsVisible)
        {
            IntVector2 mouseMove = input.MouseMove;
            yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
            pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
            pitch_ = MathDefs.Clamp(pitch_, 1.0f, 90.0f);
        }

        // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
        CameraNode.Rotation = (new Quaternion(pitch_, yaw_, 0.0f));

        // Only move the camera / show instructions if we have a controllable object
        bool showInstructions = false;
        if (clientObjectID_ != 0)
        {
            Node ballNode = Scene.GetNode(clientObjectID_);
            if (ballNode!= null)
            {
                const float CAMERA_DISTANCE = 5.0f;

                // Move camera some distance away from the ball
                CameraNode.Position = (ballNode.Position + CameraNode.Rotation * Vector3.Back * CAMERA_DISTANCE);
                showInstructions = true;
            }
        }

        instructionsText_.IsVisible = (showInstructions);
    }

    void HandlePostUpdate(StringHash eventType, VariantMap eventData)
    {
        // We only rotate the camera according to mouse movement since last frame, so do not need the time step
        MoveCamera();

        if (packetCounterTimer_.GetMSec(false) > 1000 && GetSubsystem<Network>().ServerConnection != null)
        {
            packetsIn_.SetText("Packets  in: " + GetSubsystem<Network>().ServerConnection.PacketsInPerSec);
            packetsOut_.SetText("Packets out: " + GetSubsystem<Network>().ServerConnection.PacketsOutPerSec);
            packetCounterTimer_.Reset();
        }
        if (packetCounterTimer_.GetMSec(false) > 1000 && GetSubsystem<Network>().ClientConnections.Count > 0)
        {
            int packetsIn = 0;
            int packetsOut = 0;
            var connections = GetSubsystem<Network>().ClientConnections;
            foreach (var connection in connections)
            {
                packetsIn += connection.PacketsInPerSec;
                packetsOut += connection.PacketsOutPerSec;
            }
            packetsIn_.SetText("Packets  in: " + packetsIn);
            packetsOut_.SetText("Packets out: " + packetsOut);
            packetCounterTimer_.Reset();
        }
    }

    void HandlePhysicsPreStep(StringHash eventType, VariantMap eventData)
    {
        // This function is different on the client and server. The client collects controls (WASD controls + yaw angle)
        // and sets them to its server connection object, so that they will be sent to the server automatically at a
        // fixed rate, by default 30 FPS. The server will actually apply the controls (authoritative simulation.)
        var network = GetSubsystem<Network>();
        Connection serverConnection = network.ServerConnection;

        // Client: collect controls
        if (serverConnection != null)
        {
            var ui = GetSubsystem<UI>();
            var input = GetSubsystem<Input>();
            Controls controls = new Controls();

            // Copy mouse yaw
            controls.Yaw = yaw_;

            // Only apply WASD controls if there is no focused UI element
            if (ui.GetFocusElement() != null)
            {
                controls.Set(CTRL_FORWARD, input.GetKeyDown(Key.KeyW));
                controls.Set(CTRL_BACK, input.GetKeyDown(Key.KeyS));
                controls.Set(CTRL_LEFT, input.GetKeyDown(Key.KeyA));
                controls.Set(CTRL_RIGHT, input.GetKeyDown(Key.KeyD));
            }

            serverConnection.SetControls(controls);
            // In case the server wants to do position-based interest management using the NetworkPriority components, we should also
            // tell it our observer (camera) position. In this sample it is not in use, but eg. the NinjaSnowWar game uses it
            serverConnection.Position = (CameraNode.Position);
        }
        // Server: apply controls to client objects
        else if (network.IsServerRunning)
        {
            var connections = network.ClientConnections;

            for (int i = 0; i < connections.Count; ++i)
            {
                Connection connection = connections[i];
                // Get the object this connection is controlling
                Node ballNode = serverObjects_[connection];
                if (ballNode == null)
                    continue;

                var body = ballNode.GetComponent<RigidBody>();

                // Get the last controls sent by the client
                Controls controls = connection.Controls;
                // Torque is relative to the forward vector
                Quaternion rotation = new Quaternion(0.0f, controls.Yaw, 0.0f);

            const float MOVE_TORQUE = 3.0f;

            // Movement torque is applied before each simulation step, which happen at 60 FPS. This makes the simulation
            // independent from rendering framerate. We could also apply forces (which would enable in-air control),
            // but want to emphasize that it's a ball which should only control its motion by rolling along the ground
            if ((controls.Buttons & CTRL_FORWARD) != 0)
                body.ApplyTorque(rotation * Vector3.Right * MOVE_TORQUE);
            if ((controls.Buttons & CTRL_BACK) != 0)
                body.ApplyTorque(rotation * Vector3.Left * MOVE_TORQUE);
            if ((controls.Buttons & CTRL_LEFT) != 0)
                body.ApplyTorque(rotation * Vector3.Forward * MOVE_TORQUE);
            if ((controls.Buttons & CTRL_RIGHT) != 0)
                body.ApplyTorque(rotation * Vector3.Back * MOVE_TORQUE);
        }
    }
}

void HandleConnect(StringHash eventType, VariantMap eventData)
{
    var network = GetSubsystem<Network>();
    String address = textEdit_.Text.Trim();
    if (string.IsNullOrWhiteSpace(address))
        address = "localhost"; // Use localhost to connect if nothing else specified

    // Connect to server, specify scene to use as a client for replication
    clientObjectID_ = 0; // Reset own object ID from possible previous connection
    network.Connect(address, SERVER_PORT, Scene);

    UpdateButtons();
}

void HandleDisconnect(StringHash eventType, VariantMap eventData)
{
    var network = GetSubsystem<Network>();
    Connection serverConnection = network.ServerConnection;
    // If we were connected to server, disconnect. Or if we were running a server, stop it. In both cases clear the
    // scene of all replicated content, but let the local nodes & components (the static world + camera) stay
    if (serverConnection != null)
    {
        serverConnection.Disconnect();
        Scene.Clear(true, false);
        clientObjectID_ = 0;
    }
    // Or if we were running a server, stop it
    else if (network.IsServerRunning)
    {
        network.StopServer();
        Scene.Clear(true, false);
    }

    UpdateButtons();
}

void HandleStartServer(StringHash eventType, VariantMap eventData)
{
    var network = GetSubsystem<Network>();
    network.StartServer(SERVER_PORT);

    UpdateButtons();
}

void HandleConnectionStatus(StringHash eventType, VariantMap eventData)
{
    UpdateButtons();
}

void HandleClientConnected(StringHash eventType, VariantMap eventData)
{
    // When a client connects, assign to scene to begin scene replication
    var newConnection = (Connection)(eventData[E.ClientConnected.Connection].Ptr);
newConnection.Scene = (Scene);

// Then create a controllable object for that client
Node newObject = CreateControllableObject();
serverObjects_[newConnection] = newObject;

    // Finally send the object's node ID using a remote event
    VariantMap remoteEventData = new VariantMap();
    remoteEventData[P_ID] = newObject.Id;
    newConnection.SendRemoteEvent(E_CLIENTOBJECTID, true, remoteEventData);
}

void HandleClientDisconnected(StringHash eventType, VariantMap eventData)
{
    // When a client disconnects, remove the controlled object
    var connection = (Connection)(eventData[E.ClientConnected.Connection].Ptr);
    Node @object = serverObjects_[connection];
    if (@object != null)
        @object.Remove();

serverObjects_.Remove(connection);
}

void HandleClientObjectID(StringHash eventType, VariantMap eventData)
{
    clientObjectID_ = eventData[P_ID].UInt;
}

    }

}