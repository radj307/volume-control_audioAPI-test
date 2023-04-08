using Audio;
using CoreAudio;
using WPF;

namespace volume_control_audioAPI_test
{
    public class AudioDeviceManagerVM
    {
        public AudioDeviceManagerVM()
        {
            AudioDeviceManager = new(DataFlow.Render);

            Devices = new();

            AudioDeviceManager.DeviceAddedToList += this.AudioDeviceManager_DeviceAddedToList;
            AudioDeviceManager.DeviceRemovedFromList += this.AudioDeviceManager_DeviceRemovedFromList;

            Devices.AddRange(AudioDeviceManager.Devices);
        }

        private void AudioDeviceManager_DeviceAddedToList(object? sender, AudioDevice e)
            => Devices.Add(e);
        private void AudioDeviceManager_DeviceRemovedFromList(object? sender, AudioDevice e)
            => Devices.Remove(e);

        #region Fields
        public readonly AudioDeviceManager AudioDeviceManager;
        #endregion Fields

        #region Properties
        public ObservableImmutableList<AudioDevice> Devices { get; }
        #endregion Properties
    }
}
