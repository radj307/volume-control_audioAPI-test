using Audio;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using VolumeControl.WPF;
using WPF;

namespace volume_control_audioAPI_test.ViewModels
{
    /// <summary>
    /// ViewModel for the <see cref="Audio.AudioDevice"/> class.
    /// </summary>
    public class AudioDeviceVM : INotifyPropertyChanged, IDisposable
    {
        public AudioDeviceVM(AudioDevice audioDevice)
        {
            AudioDevice = audioDevice;

            Icon = IconExtractor.TryExtractFromPath(AudioDevice.IconPath, out ImageSource icon) ? icon : null;

            Sessions = new();

            AudioDevice.SessionManager.SessionAddedToList += this.SessionManager_SessionAddedToList;
            AudioDevice.SessionManager.SessionRemovedFromList += this.SessionManager_SessionRemovedFromList;

            foreach (var session in AudioDevice.SessionManager.Sessions)
            { // initialize Sessions list:
                Sessions.Add(new AudioSessionVM(session));
            }
        }

        private void SessionManager_SessionAddedToList(object? sender, AudioSession e)
        {
            Sessions.Add(new AudioSessionVM(e));
        }
        private void SessionManager_SessionRemovedFromList(object? sender, AudioSession e)
        {
            var vm = Sessions.First(svm => svm.AudioSession.Equals(e));
            Sessions.Remove(vm);
            vm.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));

        public AudioDevice AudioDevice { get; }
        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                NotifyPropertyChanged();
            }
        }
        private ImageSource? _icon = null;
        public ObservableImmutableList<AudioSessionVM> Sessions { get; }

        public void Dispose()
        {
            this.AudioDevice.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
