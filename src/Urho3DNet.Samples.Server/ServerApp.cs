﻿using System.Diagnostics;
using System.IO;

namespace Urho3DNet.Samples.Server
{
    public class ServerApp : Application
    {
        private readonly SharedPtr<Sample> runningSample_ = new SharedPtr<Sample>(null);

        public ServerApp(Context context) : base(context)
        {
        }
        public override void Setup()
        {
            EngineParameters[Urho3D.EpHeadless] = true;
            EngineParameters[Urho3D.EpFrameLimiter] = true;
            EngineParameters[Urho3D.EpRefreshRate] = 60;
            EngineParameters[Urho3D.EpWindowTitle] = "Server";
            EngineParameters[Urho3D.EpResourcePrefixPaths] = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            base.Setup();
        }
        public override void Start()
        {
            Context.RegisterFactory<SceneReplication>(); //17
            runningSample_.Value = new SceneReplication(Context, true);
            runningSample_.Value?.Start();
            base.Start();
        }

        public override void Stop()
        {
            StopRunningSample();

            Context.Engine.DumpResources(true);
            base.Stop();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }


        private void StopRunningSample()
        {
            Sample sample = runningSample_;
            if (sample != null)
            {
                sample.Stop();
                runningSample_.Dispose();
            }
        }
    }
}