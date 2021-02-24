using Urho3DNet;

namespace Urho3DNet.Samples
{
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            Launcher.Run(_ => new SamplesManager(_));
        }
    }
}