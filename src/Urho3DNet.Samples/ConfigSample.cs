namespace Urho3DNet.Samples
{
    [Preserve(AllMembers = true)]
    internal class ConfigSample : Sample
    {
        public ConfigSample(Context context) : base(context)
        {
            
        }

        public override void Start()
        {
            // Execute base class startup
            base.Start();

            // Hook up to the frame update events
            SubscribeToEvents();

            // Set the mouse mode to use in the sample
            InitMouseMode(MouseMode.MmFree);

            LoadConfig();
            //var manager = Context.GetSubsystem<ConfigManager>();
            //var config = (SampleConfigFile)manager.Get(nameof(SampleConfigFile));
            //config.Checkbox = true;
            //manager.SaveAll();
        }

        private void SubscribeToEvents()
        {
            // Subscribe HandleUpdate() function for processing update events
            SubscribeToEvent(E.Update, HandleUpdate);
        }


        private void HandleUpdate(StringHash eventType, VariantMap eventData)
        {
            var fileSystem = Context.GetSubsystem<FileSystem>();
            var appPreferencesDir = fileSystem.GetAppPreferencesDir("App","Pref");
            ImGuiNet.ImGui.LabelText(appPreferencesDir, "");
            if (ImGuiNet.ImGui.Button("Load"))
            {
                LoadConfig();
            }
            if (ImGuiNet.ImGui.Button("Save"))
            {
                SaveConfig();
            }

            ImGuiNet.ImGui.Checkbox("Checkbox", ref _checkbox);
        }

        private void SaveConfig()
        {
            var manager = Context.GetSubsystem<ConfigManager>();
            var config = (SampleConfigFile)manager.Get(nameof(SampleConfigFile));
            config.Checkbox = _checkbox;
            manager.SaveAll();
        }

        private void LoadConfig()
        {
            var manager = Context.GetSubsystem<ConfigManager>();
            var config = (SampleConfigFile)manager.Get(nameof(SampleConfigFile));
            _checkbox = config.Checkbox;
        }

        private bool _checkbox;
    }
}