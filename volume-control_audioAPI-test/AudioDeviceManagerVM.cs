using Audio;
using CoreAudio;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using WPF;

namespace volume_control_audioAPI_test
{
    public class AudioSessionVM
    {
        public AudioSessionVM(AudioSession audioSession)
        {
            this.AudioSession = audioSession;
        }

        public AudioSession AudioSession { get; }
    }
    public class AudioDeviceManagerVM : DependencyObject, INotifyPropertyChanged
    {
        public AudioDeviceManagerVM()
        {
            AudioDeviceManager = new(DataFlow.Render);

            Devices = new();

            AudioDeviceManager.DeviceAddedToList += this.AudioDeviceManager_DeviceAddedToList;
            AudioDeviceManager.DeviceRemovedFromList += this.AudioDeviceManager_DeviceRemovedFromList;

            Devices.AddRange(AudioDeviceManager.Devices);

            Sessions = new();
        }

        private void AudioDeviceManager_DeviceAddedToList(object? sender, AudioDevice e)
            => Devices.Add(e);
        private void AudioDeviceManager_DeviceRemovedFromList(object? sender, AudioDevice e)
            => Devices.Remove(e);
        private void SessionManager_SessionAddedToList(object? sender, AudioSession e)
            => Sessions.Add(e);
        private void SessionManager_SessionRemovedFromList(object? sender, AudioSession e)
            => Sessions.Remove(e);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));

        #region Fields
        public readonly AudioDeviceManager AudioDeviceManager;
        #endregion Fields

        #region Properties
        public ObservableImmutableList<AudioDevice> Devices { get; }
        public AudioDevice? SelectedDevice
        {
            get => _selectedDevice;
            set
            {
                // disconnect previous selected device
                if (_selectedDevice is not null)
                {
                    // disconnect event handlers
                    _selectedDevice.SessionManager.SessionAddedToList -= this.SessionManager_SessionAddedToList;
                    _selectedDevice.SessionManager.SessionRemovedFromList -= this.SessionManager_SessionRemovedFromList;
                    // clear the Sessions list
                    Sessions.Clear();
                }

                // set new selected device
                _selectedDevice = value;

                // connect new selected device & update sessions
                if (_selectedDevice is not null)
                {
                    // connect event handlers
                    _selectedDevice.SessionManager.SessionAddedToList += this.SessionManager_SessionAddedToList;
                    _selectedDevice.SessionManager.SessionRemovedFromList += this.SessionManager_SessionRemovedFromList;
                    // populate the sessions list
                    foreach (var session in _selectedDevice.SessionManager.Sessions)
                    {
                        Sessions.Add(new(session));
                    }
                }

                // notify that SelectedDevice has changed
                NotifyPropertyChanged();
            }
        }
        private AudioDevice? _selectedDevice;
        public ObservableImmutableList<AudioSessionVM> Sessions { get; }
        #endregion Properties
    }
}
