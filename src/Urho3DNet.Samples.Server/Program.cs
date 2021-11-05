using System;
using System.IO;
using System.Runtime.CompilerServices;

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
            Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(Program).Assembly.Location));
            Launcher.Run(_ => new ServerApp(_));
        }
    }
}
