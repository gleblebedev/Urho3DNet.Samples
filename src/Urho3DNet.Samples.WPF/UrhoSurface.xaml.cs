using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Urho3DNet;
using UserControl = System.Windows.Controls.UserControl;

namespace Urho3DNet.Samples
{
    /// <summary>
    ///     Interaction logic for UrhoSurface.xaml
    /// </summary>
    public partial class UrhoSurface : UserControl
    {
        private Context _context;
        private Urho3DNet.Application _application;

        public UrhoSurface()
        {
            InitializeComponent();
            this.Unloaded += CloseApp;
        }

        private void CloseApp(object sender, EventArgs e)
        {
            _context.Engine.Exit();
            _application = null;
        }

        public async Task RunAsync(Func<Context, Urho3DNet.Application> func)
        {
            var panel = new System.Windows.Forms.Panel();
            _host.Child = panel;
            await Task.Yield();
            panel.Dock = DockStyle.Fill;
            Launcher.Run(_ =>
            {
                _context = _;
                _application = func(_);
                return _application;
            }, panel.Handle);
        }
    }
}