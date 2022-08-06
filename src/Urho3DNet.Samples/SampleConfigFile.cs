namespace Urho3DNet.Samples
{
    public class SampleConfigFile: ConfigFile
    {
        private bool _checkbox;

        public SampleConfigFile(Context context) : base(context)
        {
        }

        public bool Checkbox
        {
            get => _checkbox;
            set => _checkbox = value;
        }

        public override void SerializeInBlock(Archive archive)
        {
            archive.Serialize("checkbox", ref _checkbox);
        }
    }
}