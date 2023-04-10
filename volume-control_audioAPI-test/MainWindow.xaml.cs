using System.Windows;
using volume_control_audioAPI_test.ViewModels;
using VolumeControl.Log;

namespace volume_control_audioAPI_test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private AudioDeviceManagerVM AudioDeviceManagerVM => (this.FindResource("AudioDeviceManagerVM") as AudioDeviceManagerVM)!;
        private static LogWriter Log => FLog.Log;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug(new string('-', 120));
        }
    }
}
