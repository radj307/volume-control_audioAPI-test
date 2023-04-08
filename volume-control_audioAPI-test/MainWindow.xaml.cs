using System.Windows;

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
    }
}
