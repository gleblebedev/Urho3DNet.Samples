using System.Windows;

namespace Urho3DNet.Samples
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            System.Windows.MessageBox.Show("This sample is under construction.", "Warning");
            InitializeComponent();
            this.Loaded += Launch;
        }

        private async void Launch(object sender, RoutedEventArgs e)
        {
            await _urhoSurface.RunAsync(_ => new SamplesManager(_));
        }
    }
}
