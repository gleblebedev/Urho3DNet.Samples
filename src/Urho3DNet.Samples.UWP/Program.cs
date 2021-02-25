using System;
using Urho3DNet;

namespace Urho3DNet.Samples
{
    internal class Program
    {
        /// <summary>
        ///     Defines the entry point of the application.
        /// </summary>
        [MTAThread]
        private static void Main()
        {
            Launcher.SdlHandleBackButton = true;
            Launcher.Run(_ => new SamplesManager(_));
        }
    }
}