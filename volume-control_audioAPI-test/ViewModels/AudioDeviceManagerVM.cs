using Audio;
using CoreAudio;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;
using VolumeControl.Log;
using WPF;

namespace volume_control_audioAPI_test.ViewModels
{
    /// <summary>
    /// ViewModel for the <see cref="Audio.AudioDeviceManager"/> class.
    /// </summary>
    public class AudioDeviceManagerVM : DependencyObject, INotifyPropertyChanged
    {
        public AudioDeviceManagerVM()
        {
            AudioDeviceManager = new(DataFlow.Render);

            Devices = new();

            AudioDeviceManager.DeviceAddedToList += AudioDeviceManager_DeviceAddedToList;
            AudioDeviceManager.DeviceRemovedFromList += AudioDeviceManager_DeviceRemovedFromList;

            foreach (var device in AudioDeviceManager.Devices)
            {
                Devices.Add(new(device));
            }

            Sessions = new();
            AllSessions = new();

            AudioSessionManager = new();

            AudioSessionManager.SessionAddedToList += this.AudioSessionManager_SessionAddedToList;
            AudioSessionManager.SessionRemovedFromList += this.AudioSessionManager_SessionRemovedFromList;

            AudioSessionManager.AddSessionManagers(Devices.Select(d => d.AudioDevice.SessionManager));
        }


        private void AudioSessionManager_SessionAddedToList(object? sender, AudioSession e)
            => Dispatcher.Invoke(() => AllSessions.Add(new AudioSessionVM(e)));
        private void AudioSessionManager_SessionRemovedFromList(object? sender, AudioSession e)
        {
            var vm = AllSessions.First(svm => svm.AudioSession.Equals(e));
            AllSessions.Remove(vm);
            vm.Dispose();
        }

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Fields
        public readonly AudioDeviceManager AudioDeviceManager;
        #endregion Fields

        #region Properties
        private static LogWriter Log => FLog.Log;
        public ObservableImmutableList<AudioDeviceVM> Devices { get; }
        public AudioDeviceVM? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                // disconnect previous selected device
                if (_selectedDevice is not null)
                {
                    // disconnect event handlers
                    _selectedDevice.AudioDevice.SessionManager.SessionAddedToList -= SessionManager_SessionAddedToList;
                    _selectedDevice.AudioDevice.SessionManager.SessionRemovedFromList -= SessionManager_SessionRemovedFromList;
                    // clear the Sessions list
                    Sessions.Clear();
                }

                // set new selected device
                _selectedDevice = value;

                // connect new selected device & update sessions
                if (_selectedDevice is not null)
                {
                    // connect event handlers
                    _selectedDevice.AudioDevice.SessionManager.SessionAddedToList += SessionManager_SessionAddedToList;
                    _selectedDevice.AudioDevice.SessionManager.SessionRemovedFromList += SessionManager_SessionRemovedFromList;
                    // populate the sessions list
                    foreach (var session in _selectedDevice.AudioDevice.SessionManager.Sessions)
                    {
                        Sessions.Add(new(session));
                    }
                }

                // notify that SelectedDevice has changed
                NotifyPropertyChanged();
            }
        }
        private AudioDeviceVM? _selectedDevice;
        public ObservableImmutableList<AudioSessionVM> Sessions { get; }
        public Audio.AudioSessionManager AudioSessionManager { get; }
        public ObservableImmutableList<AudioSessionVM> AllSessions { get; }
        #endregion Properties

        #region Methods (EventHandlers)
        private void AudioDeviceManager_DeviceAddedToList(object? sender, AudioDevice e)
            => Dispatcher.Invoke(() => Devices.Add(new AudioDeviceVM(e)));
        private void AudioDeviceManager_DeviceRemovedFromList(object? sender, AudioDevice e)
        {
            var vm = Devices.First(device => device.AudioDevice.Equals(e));
            Devices.Remove(vm);
            vm.Dispose();
        }
        private void SessionManager_SessionAddedToList(object? sender, AudioSession e)
            => Dispatcher.Invoke(() => Sessions.Add(new AudioSessionVM(e)));
        private void SessionManager_SessionRemovedFromList(object? sender, AudioSession e)
        {
            var vm = Sessions.First(session => session.AudioSession.Equals(e));
            Sessions.Remove(vm);
            vm.Dispose();
        }
        #endregion Methods (EventHandlers)
    }
}
