using System;

namespace Urho3DNet.Samples.Server
{
    class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Launcher.Run(_ => new ServerApp(_));
        }
    }
}
