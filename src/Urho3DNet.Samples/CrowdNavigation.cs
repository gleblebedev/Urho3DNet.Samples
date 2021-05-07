using System.Collections.Generic;

namespace Urho3DNet.Samples
{
    public class CrowdNavigation : Sample
    {
        /// Flag for using navigation mesh streaming.
        bool useStreaming_ = false;
        /// Streaming distance.
        int streamingDistance_ = 2;
        /// Tile data.
        Dictionary<IntVector2, ByteVector> tileData_ = new Dictionary<IntVector2, ByteVector>();
        /// Added tiles.
        HashSet<IntVector2> addedTiles_;
        /// Flag for drawing debug geometry.
        bool drawDebug_ = false;
        /// Instruction text UI-element.
        Text instructionText_;


        public CrowdNavigation(Context context) : base(context)
        {
        }


        //public override void Start()
        //{
        //    // Execute base class startup
        //    base.Start();

        //    // Create the scene content
        //    CreateScene();

        //    // Create the UI content
        //    CreateUI();

        //    // Setup the viewport for displaying the scene
        //    SetupViewport();

        //    // Hook up to the frame update and render post-update events
        //    SubscribeToEvents();

        //    // Set the mouse mode to use in the sample
        //    base.InitMouseMode(MouseMode.MmAbsolute);
        //}

        //void CreateScene()
        //{
        //    var cache = GetSubsystem<ResourceCache>();

        //    Scene = new Scene(Context);

        //    // Create octree, use default volume (-1000, -1000, -1000) to (1000, 1000, 1000)
        //    // Also create a DebugRenderer component so that we can draw debug geometry
        //    Scene.CreateComponent<Octree>();
        //    Scene.CreateComponent<DebugRenderer>();

        //    // Create scene node & StaticModel component for showing a static plane
        //    Node planeNode = Scene.CreateChild("Plane");
        //    planeNode.SetScale(new Vector3(100.0f, 1.0f, 100.0f));
        //    var planeObject = planeNode.CreateComponent<StaticModel>();
        //    planeObject.SetModel(cache.GetResource<Model>("Models/Plane.mdl"));
        //    planeObject.SetMaterial(cache.GetResource<Material>("Materials/StoneTiled.xml"));

        //    // Create a Zone component for ambient lighting & fog control
        //    Node zoneNode = Scene.CreateChild("Zone");
        //    var zone = zoneNode.CreateComponent<Zone>();
        //    zone.SetBoundingBox(new BoundingBox(-1000.0f, 1000.0f));
        //    zone.SetAmbientColor(new Color(0.15f, 0.15f, 0.15f));
        //    zone.SetFogColor(new Color(0.5f, 0.5f, 0.7f));
        //    zone.SetFogStart(100.0f);
        //    zone.SetFogEnd(300.0f);

        //    // Create a directional light to the world. Enable cascaded shadows on it
        //    Node lightNode = Scene.CreateChild("DirectionalLight");
        //    lightNode.SetDirection(new Vector3(0.6f, -1.0f, 0.8f));
        //    var light = lightNode.CreateComponent<Light>();
        //    light.SetLightType(LIGHT_DIRECTIONAL);
        //    light.SetCastShadows(true);
        //    light.SetShadowBias(BiasParameters(0.00025f, 0.5f));
        //    // Set cascade splits at 10, 50 and 200 world units, fade shadows out at 80% of maximum shadow distance
        //    light.SetShadowCascade(CascadeParameters(10.0f, 50.0f, 200.0f, 0.0f, 0.8f));

        //    // Create randomly sized boxes. If boxes are big enough, make them occluders
        //    Node boxGroup = Scene.CreateChild("Boxes");
        //    for (uint i = 0; i < 20; ++i)
        //    {
        //        Node boxNode = boxGroup.CreateChild("Box");
        //        float size = 1.0f + MathDefs.Random(10.0f);
        //        boxNode.SetPosition(new Vector3(MathDefs.Random(80.0f) - 40.0f, size * 0.5f, MathDefs.Random(80.0f) - 40.0f));
        //        boxNode.SetScale(size);
        //        var boxObject = boxNode.CreateComponent<StaticModel>();
        //        boxObject.SetModel(cache.GetResource<Model>("Models/Box.mdl"));
        //        boxObject.SetMaterial(cache.GetResource<Material>("Materials/Stone.xml"));
        //        boxObject.SetCastShadows(true);
        //        if (size >= 3.0f)
        //            boxObject.SetOccluder(true);
        //    }

        //    // Create a DynamicNavigationMesh component to the scene root
        //    var navMesh = Scene.CreateComponent<DynamicNavigationMesh>();
        //    // Set small tiles to show navigation mesh streaming
        //    navMesh.SetTileSize(32);
        //    // Enable drawing debug geometry for obstacles and off-mesh connections
        //    navMesh.SetDrawObstacles(true);
        //    navMesh.SetDrawOffMeshConnections(true);
        //    // Set the agent height large enough to exclude the layers under boxes
        //    navMesh.SetAgentHeight(10.0f);
        //    // Set nav mesh cell height to minimum (allows agents to be grounded)
        //    navMesh.SetCellHeight(0.05f);
        //    // Create a Navigable component to the scene root. This tags all of the geometry in the scene as being part of the
        //    // navigation mesh. By default this is recursive, but the recursion could be turned off from Navigable
        //    Scene.CreateComponent<Navigable>();
        //    // Add padding to the navigation mesh in Y-direction so that we can add objects on top of the tallest boxes
        //    // in the scene and still update the mesh correctly
        //    navMesh.SetPadding(new Vector3(0.0f, 10.0f, 0.0f));
        //    // Now build the navigation geometry. This will take some time. Note that the navigation mesh will prefer to use
        //    // physics geometry from the scene nodes, as it often is simpler, but if it can not find any (like in this example)
        //    // it will use renderable geometry instead
        //    navMesh.Build();

        //    // Create an off-mesh connection to each box to make them climbable (tiny boxes are skipped). A connection is built from 2 nodes.
        //    // Note that OffMeshConnections must be added before building the navMesh, but as we are adding Obstacles next, tiles will be varmatically rebuilt.
        //    // Creating connections post-build here allows us to use FindNearestPoint() to procedurally set accurate positions for the connection
        //    CreateBoxOffMeshConnections(navMesh, boxGroup);

        //    // Create some mushrooms as obstacles. Note that obstacles are non-walkable areas
        //    for (uint i = 0; i < 100; ++i)
        //        CreateMushroom(new Vector3(MathDefs.Random(90.0f) - 45.0f, 0.0f, MathDefs.Random(90.0f) - 45.0f));

        //    // Create a CrowdManager component to the scene root
        //    var crowdManager = Scene.CreateComponent<CrowdManager>();
        //    CrowdObstacleAvoidanceParams @params = crowdManager.GetObstacleAvoidanceParams(0);
        //    // Set the params to "High (66)" setting
        //    @params.velBias = 0.5f;
        //    @params.adaptiveDivs = 7;
        //    @params.adaptiveRings = 3;
        //    @params.adaptiveDepth = 3;
        //    crowdManager.SetObstacleAvoidanceParams(0, @params);

        //    // Create some movable barrels. We create them as crowd agents, as for moving entities it is less expensive and more convenient than using obstacles
        //    CreateMovingBarrels(navMesh);

        //    // Create Jack node as crowd agent
        //    SpawnJack(new Vector3(-5.0f, 0.0f, 20.0f), Scene.CreateChild("Jacks"));

        //    // Create the camera. Set far clip to match the fog. Note: now we actually create the camera node outside the scene, because
        //    // we want it to be unaffected by scene load / save
        //    CameraNode = new Node(Context);
        //    var camera = CameraNode.CreateComponent<Camera>();
        //    camera.SetFarClip(300.0f);

        //    // Set an initial position for the camera scene node above the plane and looking down
        //    CameraNode.SetPosition(new Vector3(0.0f, 50.0f, 0.0f));
        //    pitch_ = 80.0f;
        //    CameraNode.SetRotation(new Quaternion(pitch_, yaw_, 0.0f));
        //}

        //void CreateUI()
        //{
        //    var cache = GetSubsystem<ResourceCache>();
        //    var ui = GetSubsystem<UI>();

        //    // Create a Cursor UI element because we want to be able to hide and show it at will. When hidden, the mouse cursor will
        //    // control the camera, and when visible, it will point the raycast target
        //    var style = cache.GetResource<XMLFile>("UI/DefaultStyle.xml");
        //    SharedPtr<Cursor> cursor(new Cursor(Context));
        //    cursor.SetStyleAuto(style);
        //    ui.SetCursor(cursor);

        //    // Set starting position of the cursor at the rendering window center
        //    var graphics = GetSubsystem<Graphics>();
        //    cursor.SetPosition(graphics.Width / 2, graphics.Height / 2);

        //    // Construct new Text object, set string to display and font to use
        //    instructionText_ = ui.GetRoot().CreateChild<Text>();
        //    instructionText_.SetText(
        //        "Use WASD keys to move, RMB to rotate view\n"


        //        "LMB to set destination, SHIFT+LMB to spawn a Jack\n"


        //        "MMB or O key to add obstacles or remove obstacles/agents\n"


        //        "F5 to save scene, F7 to load\n"


        //        "Tab to toggle navigation mesh streaming\n"


        //        "Space to toggle debug geometry\n"


        //        "F12 to toggle this instruction text"
        //    );
        //    instructionText_.SetFont(cache.GetResource<Font>("Fonts/Anonymous Pro.ttf"), 15);
        //    // The text has multiple rows. Center them in relation to each other
        //    instructionText_.SetTextAlignment(HA_CENTER);

        //    // Position the text relative to the screen center
        //    instructionText_.SetHorizontalAlignment(HA_CENTER);
        //    instructionText_.SetVerticalAlignment(VA_CENTER);
        //    instructionText_.SetPosition(0, ui.GetRoot().Height / 4);
        //}

        //void SetupViewport()
        //{
        //    var renderer = GetSubsystem<Renderer>();

        //    // Set up a viewport to the Renderer subsystem so that the 3D scene can be seen
        //    SharedPtr<Viewport> viewport(new Viewport(Context, Scene, CameraNode.GetComponent<Camera>()));
        //    renderer.SetViewport(0, viewport);
        //}

        //void SubscribeToEvents()
        //{
        //    // Subscribe HandleUpdate() function for processing update events
        //    SubscribeToEvent(E_UPDATE, URHO3D_HANDLER(CrowdNavigation, HandleUpdate));

        //    // Subscribe HandlePostRenderUpdate() function for processing the post-render update event, during which we request debug geometry
        //    SubscribeToEvent(E_POSTRENDERUPDATE, URHO3D_HANDLER(CrowdNavigation, HandlePostRenderUpdate));

        //    // Subscribe HandleCrowdAgentFailure() function for resolving invalidation issues with agents, during which we
        //    // use a larger extents for finding a point on the navmesh to fix the agent's position
        //    SubscribeToEvent(E_CROWD_AGENT_FAILURE, URHO3D_HANDLER(CrowdNavigation, HandleCrowdAgentFailure));

        //    // Subscribe HandleCrowdAgentReposition() function for controlling the animation
        //    SubscribeToEvent(E_CROWD_AGENT_REPOSITION, URHO3D_HANDLER(CrowdNavigation, HandleCrowdAgentReposition));

        //    // Subscribe HandleCrowdAgentFormation() function for positioning agent into a formation
        //    SubscribeToEvent(E_CROWD_AGENT_FORMATION, URHO3D_HANDLER(CrowdNavigation, HandleCrowdAgentFormation));
        //}

        //void SpawnJack(Vector3 pos, Node jackGroup)
        //{
        //    var cache = GetSubsystem<ResourceCache>();
        //    SharedPtr<Node> jackNode(jackGroup.CreateChild("Jack"));
        //    jackNode.SetPosition(pos);
        //    var modelObject = jackNode.CreateComponent<AnimatedModel>();
        //    modelObject.SetModel(cache.GetResource<Model>("Models/Jack.mdl"));
        //    modelObject.SetMaterial(cache.GetResource<Material>("Materials/Jack.xml"));
        //    modelObject.SetCastShadows(true);
        //    jackNode.CreateComponent<AnimationController>();

        //    // Create a CrowdAgent component and set its height and realistic max speed/acceleration. Use default radius
        //    var agent = jackNode.CreateComponent<CrowdAgent>();
        //    agent.SetHeight(2.0f);
        //    agent.SetMaxSpeed(3.0f);
        //    agent.SetMaxAccel(5.0f);
        //}

        //void CreateMushroom(Vector3 pos)
        //{
        //    var cache = GetSubsystem<ResourceCache>();

        //    Node mushroomNode = Scene.CreateChild("Mushroom");
        //    mushroomNode.SetPosition(pos);
        //    mushroomNode.SetRotation(new Quaternion(0.0f, MathDefs.Random(360.0f), 0.0f));
        //    mushroomNode.SetScale(2.0f + MathDefs.Random(0.5f));
        //    var mushroomObject = mushroomNode.CreateComponent<StaticModel>();
        //    mushroomObject.SetModel(cache.GetResource<Model>("Models/Mushroom.mdl"));
        //    mushroomObject.SetMaterial(cache.GetResource<Material>("Materials/Mushroom.xml"));
        //    mushroomObject.SetCastShadows(true);

        //    // Create the navigation Obstacle component and set its height & radius proportional to scale
        //    var obstacle = mushroomNode.CreateComponent<Obstacle>();
        //    obstacle.SetRadius(mushroomNode.GetScale().X);
        //    obstacle.SetHeight(mushroomNode.GetScale().Y);
        //}

        //void CreateBoxOffMeshConnections(DynamicNavigationMesh* navMesh, Node boxGroup)
        //{
        //    const ea::vector<SharedPtr<Node>>&boxes = boxGroup.GetChildren();
        //    for (uint i = 0; i < boxes.size(); ++i)
        //    {
        //        Node box = boxes[i];
        //        Vector3 boxPos = box.GetPosition();
        //        float boxHalfSize = box.GetScale().X / 2;

        //        // Create 2 empty nodes for the start & end points of the connection. Note that order matters only when using one-way/unidirectional connection.
        //        Node connectionStart = box.CreateChild("ConnectionStart");
        //        connectionStart.SetWorldPosition(navMesh.FindNearestPoint(boxPos + Vector3(boxHalfSize, -boxHalfSize, 0))); // Base of box
        //        Node connectionEnd = connectionStart.CreateChild("ConnectionEnd");
        //        connectionEnd.SetWorldPosition(navMesh.FindNearestPoint(boxPos + Vector3(boxHalfSize, boxHalfSize, 0))); // Top of box

        //        // Create the OffMeshConnection component to one node and link the other node
        //        var connection = connectionStart.CreateComponent<OffMeshConnection>();
        //        connection.SetEndPoint(connectionEnd);
        //    }
        //}

        //void CreateMovingBarrels(DynamicNavigationMesh* navMesh)
        //{
        //    var cache = GetSubsystem<ResourceCache>();
        //    Node barrel = Scene.CreateChild("Barrel");
        //    var model = barrel.CreateComponent<StaticModel>();
        //    model.SetModel(cache.GetResource<Model>("Models/Cylinder.mdl"));
        //    var material = cache.GetResource<Material>("Materials/StoneTiled.xml");
        //    model.SetMaterial(material);
        //    material.SetTexture(TU_DIFFUSE, cache.GetResource<Texture2D>("Textures/TerrainDetail2.dds"));
        //    model.SetCastShadows(true);
        //    for (uint i = 0; i < 20; ++i)
        //    {
        //        Node clone = barrel.Clone();
        //        float size = 0.5f + MathDefs.Random(1.0f);
        //        clone.SetScale(new Vector3(size / 1.5f, size * 2.0f, size / 1.5f));
        //        clone.SetPosition(navMesh.FindNearestPoint(new Vector3(MathDefs.Random(80.0f) - 40.0f, size * 0.5f, MathDefs.Random(80.0f) - 40.0f)));
        //        var agent = clone.CreateComponent<CrowdAgent>();
        //        agent.SetRadius(clone.GetScale().X * 0.5f);
        //        agent.SetHeight(size);
        //        agent.SetNavigationQuality(NAVIGATIONQUALITY_LOW);
        //    }
        //    barrel.Remove();
        //}

        //void SetPathPoint(bool spawning)
        //{
        //    Vector3 hitPos;
        //    Drawable hitDrawable;

        //    if (Raycast(250.0f, hitPos, hitDrawable))
        //    {
        //        var navMesh = Scene.GetComponent<DynamicNavigationMesh>();
        //        Vector3 pathPos = navMesh.FindNearestPoint(hitPos, Vector3(1.0f, 1.0f, 1.0f));
        //        Node jackGroup = Scene.GetChild("Jacks");
        //        if (spawning)
        //            // Spawn a jack at the target position
        //            SpawnJack(pathPos, jackGroup);
        //        else
        //            // Set crowd agents target position
        //            Scene.GetComponent<CrowdManager>().SetCrowdTarget(pathPos, jackGroup);
        //    }
        //}

        //void AddOrRemoveObject()
        //{
        //    // Raycast and check if we hit a mushroom node. If yes, remove it, if no, create a new one
        //    Vector3 hitPos;
        //    Drawable hitDrawable;

        //    if (Raycast(250.0f, hitPos, hitDrawable))
        //    {
        //        Node hitNode = hitDrawable.GetNode();

        //        // Note that navmesh rebuild happens when the Obstacle component is removed
        //        if (hitNode.GetName() == "Mushroom")
        //            hitNode.Remove();
        //        else if (hitNode.GetName() == "Jack")
        //            hitNode.Remove();
        //        else
        //            CreateMushroom(hitPos);
        //    }
        //}

        //bool Raycast(float maxDistance, Vector3& hitPos, Drawable& hitDrawable)
        //{
        //    hitDrawable = null;

        //    var ui = GetSubsystem<UI>();
        //    IntVector2 pos = ui.GetCursorPosition();
        //    // Check the cursor is visible and there is no UI element in front of the cursor
        //    if (!ui.Cursor.IsVisible() || ui.GetElementAt(pos, true))
        //        return false;

        //    pos = ui.ConvertUIToSystem(pos);

        //    var graphics = GetSubsystem<Graphics>();
        //    var camera = CameraNode.GetComponent<Camera>();
        //    Ray cameraRay = camera.GetScreenRay((float)pos.X / graphics.Width, (float)pos.Y / graphics.Height);
        //    // Pick only geometry objects, not eg. zones or lights, only get the first (closest) hit
        //    ea::vector<RayQueryResult> results;
        //    RayOctreeQuery query(results, cameraRay, RAY_TRIANGLE, maxDistance, DRAWABLE_GEOMETRY);
        //    Scene.GetComponent<Octree>().RaycastSingle(query);
        //    if (results.size())
        //    {
        //        RayQueryResult result = results[0];
        //        hitPos = result.position_;
        //        hitDrawable = result.drawable_;
        //        return true;
        //    }

        //    return false;
        //}

        //void MoveCamera(float timeStep)
        //{
        //    // Right mouse button controls mouse cursor visibility: hide when pressed
        //    var ui = GetSubsystem<UI>();
        //    var input = GetSubsystem<Input>();
        //    ui.Cursor.SetVisible(!input.GetMouseButtonDown(MOUSEB_RIGHT));

        //    // Do not move if the UI has a focused element (the console)
        //    if (ui.GetFocusElement())
        //        return;

        //    // Movement speed as world units per second
        //    const float MOVE_SPEED = 20.0f;
        //    // Mouse sensitivity as degrees per pixel
        //    const float MOUSE_SENSITIVITY = 0.1f;

        //    // Use this frame's mouse motion to adjust camera node yaw and pitch. Clamp the pitch between -90 and 90 degrees
        //    // Only move the camera when the cursor is hidden
        //    if (!ui.Cursor.IsVisible())
        //    {
        //        IntVector2 mouseMove = input.GetMouseMove();
        //        yaw_ += MOUSE_SENSITIVITY * mouseMove.X;
        //        pitch_ += MOUSE_SENSITIVITY * mouseMove.Y;
        //        pitch_ = Clamp(pitch_, -90.0f, 90.0f);

        //        // Construct new orientation for the camera scene node from yaw and pitch. Roll is fixed to zero
        //        CameraNode.SetRotation(new Quaternion(pitch_, yaw_, 0.0f));
        //    }

        //    // Read WASD keys and move the camera scene node to the corresponding direction if they are pressed
        //    if (input.GetKeyDown(Key.KeyW))
        //        CameraNode.Translate(new Vector3.FORWARD* MOVE_SPEED * timeStep);
        //    if (input.GetKeyDown(Key.KeyS))
        //        CameraNode.Translate(new Vector3.BACK* MOVE_SPEED * timeStep);
        //    if (input.GetKeyDown(Key.KeyA))
        //        CameraNode.Translate(new Vector3.LEFT* MOVE_SPEED * timeStep);
        //    if (input.GetKeyDown(Key.KeyD))
        //        CameraNode.Translate(new Vector3.RIGHT* MOVE_SPEED * timeStep);

        //    // Set destination or spawn a new jack with left mouse button
        //    if (input.GetMouseButtonPress(MOUSEB_LEFT))
        //        SetPathPoint(input.GetQualifierDown(QUAL_SHIFT));
        //    // Add new obstacle or remove existing obstacle/agent with middle mouse button
        //    else if (input.GetMouseButtonPress(MOUSEB_MIDDLE) || input.GetKeyPress(Key.KeyO))
        //        AddOrRemoveObject();

        //    // Check for loading/saving the scene from/to the file Data/Scenes/CrowdNavigation.xml relative to the executable directory
        //    if (input.GetKeyPress(Key.KeyF5))
        //    {
        //        File saveFile(Context, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CrowdNavigation.xml", FILE_WRITE);
        //        Scene.SaveXML(saveFile);
        //    }
        //    else if (input.GetKeyPress(Key.KeyF7))
        //    {
        //        File loadFile(Context, GetSubsystem<FileSystem>().GetProgramDir() +"Data/Scenes/CrowdNavigation.xml", FILE_READ);
        //        Scene.LoadXML(loadFile);
        //    }

        //    // Toggle debug geometry with space
        //    else if (input.GetKeyPress(Key.KeySPACE))
        //        drawDebug_ = !drawDebug_;

        //    // Toggle instruction text with F12
        //    else if (input.GetKeyPress(Key.KeyF12))
        //    {
        //        if (instructionText_)
        //            instructionText_.SetVisible(!instructionText_.IsVisible());
        //    }
        //}

        //void ToggleStreaming(bool enabled)
        //{
        //    var navMesh = Scene.GetComponent<DynamicNavigationMesh>();
        //    if (enabled)
        //    {
        //        int maxTiles = (2 * streamingDistance_ + 1) * (2 * streamingDistance_ + 1);
        //        BoundingBox boundingBox = navMesh.GetBoundingBox();
        //        SaveNavigationData();
        //        navMesh.Allocate(boundingBox, maxTiles);
        //    }
        //    else
        //        navMesh.Build();
        //}

        //void UpdateStreaming()
        //{
        //    // Center the navigation mesh at the crowd of jacks
        //    Vector3 averageJackPosition;
        //    if (Node jackGroup = Scene.GetChild("Jacks"))
        //{
        //        const uint numJacks = jackGroup.GetNumChildren();
        //        for (uint i = 0; i < numJacks; ++i)
        //            averageJackPosition += jackGroup.GetChild(i).GetWorldPosition();
        //        averageJackPosition /= (float)numJacks;
        //    }

        //    // Compute currently loaded area
        //    var navMesh = Scene.GetComponent<DynamicNavigationMesh>();
        //    IntVector2 jackTile = navMesh.GetTileIndex(averageJackPosition);
        //    IntVector2 numTiles = navMesh.GetNumTiles();
        //    IntVector2 beginTile = VectorMax(IntVector2::ZERO, jackTile - IntVector2::ONE * streamingDistance_);
        //    IntVector2 endTile = VectorMin(jackTile + IntVector2::ONE * streamingDistance_, numTiles - IntVector2::ONE);

        //    // Remove tiles
        //    for (var i = addedTiles_.begin(); i != addedTiles_.end();)
        //    {
        //        IntVector2 tileIdx = *i;
        //        if (beginTile.X <= tileIdx.X && tileIdx.X <= endTile.X && beginTile.Y <= tileIdx.Y && tileIdx.Y <= endTile.Y)
        //            ++i;
        //        else
        //        {
        //            navMesh.RemoveTile(tileIdx);
        //            i = addedTiles_.erase(i);
        //        }
        //    }

        //    // Add tiles
        //    for (int z = beginTile.Y; z <= endTile.Y; ++z)
        //        for (int x = beginTile.X; x <= endTile.X; ++x)
        //        {
        //            const IntVector2 tileIdx(x, z);
        //            if (!navMesh.HasTile(tileIdx) && tileData_.contains(tileIdx))
        //            {
        //                addedTiles_.insert(tileIdx);
        //                navMesh.AddTile(tileData_[tileIdx]);
        //            }
        //        }
        //}

        //void SaveNavigationData()
        //{
        //    var navMesh = Scene.GetComponent<DynamicNavigationMesh>();
        //    tileData_.clear();
        //    addedTiles_.clear();
        //    const IntVector2 numTiles = navMesh.GetNumTiles();
        //    for (int z = 0; z < numTiles.Y; ++z)
        //        for (int x = 0; x <= numTiles.X; ++x)
        //        {
        //            const IntVector2 tileIdx = IntVector2(x, z);
        //            tileData_[tileIdx] = navMesh.GetTileData(tileIdx);
        //        }
        //}

        //void HandleUpdate(StringHash eventType, VariantMap eventData)
        //{

        //    // Take the frame time step, which is stored as a float
        //    float timeStep = eventData[P_TIMESTEP].GetFloat();

        //    // Move the camera, scale movement with time step
        //    MoveCamera(timeStep);

        //    // Update streaming
        //    var input = GetSubsystem<Input>();
        //    if (input.GetKeyPress(Key.KeyTAB))
        //    {
        //        useStreaming_ = !useStreaming_;
        //        ToggleStreaming(useStreaming_);
        //    }
        //    if (useStreaming_)
        //        UpdateStreaming();

        //}

        //void HandlePostRenderUpdate(StringHash eventType, VariantMap eventData)
        //{
        //    if (drawDebug_)
        //    {
        //        // Visualize navigation mesh, obstacles and off-mesh connections
        //        Scene.GetComponent<DynamicNavigationMesh>().DrawDebugGeometry(true);
        //        // Visualize agents' path and position to reach
        //        Scene.GetComponent<CrowdManager>().DrawDebugGeometry(true);
        //    }
        //}

        //void HandleCrowdAgentFailure(StringHash eventType, VariantMap eventData)
        //{

        //    var node = static_cast<Node>(eventData[P_NODE].GetPtr());
        //    var agentState = (CrowdAgentState)eventData[P_CROWD_AGENT_STATE].GetInt();

        //    // If the agent's state is invalid, likely from spawning on the side of a box, find a point in a larger area
        //    if (agentState == CA_STATE_INVALID)
        //    {
        //        // Get a point on the navmesh using more generous extents
        //        Vector3 newPos = Scene.GetComponent<DynamicNavigationMesh>().FindNearestPoint(node.GetPosition(), Vector3(5.0f, 5.0f, 5.0f));
        //        // Set the new node position, CrowdAgent component will varmatically reset the state of the agent
        //        node.SetPosition(newPos);
        //    }
        //}

        //void HandleCrowdAgentReposition(StringHash eventType, VariantMap eventData)
        //{
        //    string WALKING_ANI = "Models/Jack_Walk.ani";


        //    var node = static_cast<Node>(eventData[P_NODE].GetPtr());
        //    var agent = static_cast<CrowdAgent*>(eventData[P_CROWD_AGENT].GetPtr());
        //    Vector3 velocity = eventData[P_VELOCITY].GetVector3();
        //    float timeStep = eventData[P_TIMESTEP].GetFloat();

        //    // Only Jack agent has animation controller
        //    var animCtrl = node.GetComponent<AnimationController>();
        //    if (animCtrl)
        //    {
        //        float speed = velocity.Length();
        //        if (animCtrl.IsPlaying(WALKING_ANI))
        //        {
        //            float speedRatio = speed / agent.GetMaxSpeed();
        //            // Face the direction of its velocity but moderate the turning speed based on the speed ratio and timeStep
        //            node.SetRotation(node.GetRotation().Slerp(new Quaternion(new Vector3.FORWARD, velocity), 10.0f * timeStep * speedRatio));
        //            // Throttle the animation speed based on agent speed ratio (ratio = 1 is full throttle)
        //            animCtrl.SetSpeed(WALKING_ANI, speedRatio * 1.5f);
        //        }
        //        else
        //            animCtrl.Play(WALKING_ANI, 0, true, 0.1f);

        //        // If speed is too low then stop the animation
        //        if (speed < agent.GetRadius())
        //            animCtrl.Stop(WALKING_ANI, 0.5f);
        //    }
        //}

        //void HandleCrowdAgentFormation(StringHash eventType, VariantMap eventData)
        //{

        //    uint index = eventData[P_INDEX].GetUInt();
        //    uint size = eventData[P_SIZE].GetUInt();
        //    Vector3 position = eventData[P_POSITION].GetVector3();

        //    // The first agent will always move to the exact position, all other agents will select a random point nearby
        //    if (index)
        //    {
        //        var crowdManager = static_cast<CrowdManager*>(GetEventSender());
        //        var agent = static_cast<CrowdAgent*>(eventData[P_CROWD_AGENT].GetPtr());
        //        eventData[P_POSITION] = crowdManager.GetMathDefs.RandomPointInCircle(position, agent.GetRadius(), agent.GetQueryFilterType());
        //    }
        //}

    }
}