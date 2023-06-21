using Audio;
using Input;
using System;
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

            TestHotkey = new(System.Windows.Input.Key.W, EModifier.Ctrl | EModifier.Alt | EModifier.Shift, true);
            TestHotkey.Pressed += (s, e) =>
            {
                if (AudioDeviceManagerVM.SelectedSession is AudioSessionVM session)
                {
                    session.AudioSession.Mute = !session.AudioSession.Mute;
                }
                else if (AudioDeviceManagerVM.SelectedDevice is AudioDeviceVM device)
                {
                    device.AudioDevice.Mute = !device.AudioDevice.Mute;
                }
            };
        }

        private AudioDeviceManagerVM AudioDeviceManagerVM => (this.FindResource("AudioDeviceManagerVM") as AudioDeviceManagerVM)!;
        private static LogWriter Log => FLog.Log;

        private readonly Hotkey TestHotkey;

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Log.Debug(new string('-', 120));
        }
    }
}
