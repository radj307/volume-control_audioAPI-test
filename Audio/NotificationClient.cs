using CoreAudio;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VolumeControl.Log;

namespace Audio
{
    /// <summary>
    /// An audio endpoint device.
    /// </summary>
    public class AudioDevice : IVolumeControl, IReadOnlyVolumeControl, INotifyPropertyChanged, IDisposable
    {
        #region Constructor
        internal AudioDevice(MMDevice mmDevice)
        {
            MMDevice = mmDevice;
            
            Name = mmDevice.GetDeviceName();
            ID = mmDevice.ID;

            if (MMDevice.AudioSessionManager2 is null)
            {
                throw new NullReferenceException($"{nameof(AudioDevice)} '{Name}' has a null {nameof(MMDevice.AudioSessionManager2)} property!");
            }
            if (MMDevice.AudioEndpointVolume is null)
            {
                throw new NullReferenceException($"{nameof(AudioDevice)} '{Name}' has a null {nameof(MMDevice.AudioEndpointVolume)} property!");
            }
            if (MMDevice.AudioMeterInformation is null)
            {
                throw new NullReferenceException($"{nameof(AudioDevice)} '{Name}' has a null {nameof(MMDevice.AudioMeterInformation)} property!");
            }

            AudioEndpointVolume.OnVolumeNotification += this.AudioEndpointVolume_OnVolumeNotification;
        }
        #endregion Constructor

        #region Events
        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        /// <summary>
        /// Occurs when this <see cref="AudioDevice"/> instance's volume or mute state was changed.
        /// </summary>
        public event VolumeChangedEventHandler? VolumeChanged;
        private void NotifyVolumeChanged(AudioVolumeNotificationData data) => VolumeChanged?.Invoke(this, new(data));
        #endregion Events

        #region Fields
        /// <summary>
        /// Used to sequence calls to the <see cref="AudioEndpointVolume"/> property.
        /// </summary>
        private readonly object lock_AudioEndpointVolume = new();
        private bool disposedValue;
        #endregion Fields

        #region Properties
        private static LogWriter Log => FLog.Log;
        internal MMDevice MMDevice { get; }
        internal AudioEndpointVolume AudioEndpointVolume
        {
            get
            {
                lock (lock_AudioEndpointVolume)
                {
                    return MMDevice.AudioEndpointVolume!;
                }
            }
        }
        internal AudioSessionManager2 AudioSessionManager => MMDevice.AudioSessionManager2!;

        /// <summary>
        /// Gets the name of this <see cref="AudioDevice"/> instance as shown in Windows.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the ID string of this <see cref="AudioDevice"/> instance.
        /// </summary>
        public string ID { get; }

        #region Properties (IVolumeControl)
        /// <inheritdoc/>
        public float NativeVolume
        {
            get => AudioEndpointVolume.MasterVolumeLevelScalar;
            set
            {
                if (value < 0.0f)
                    value = 0.0f;
                else if (value > 1.0f)
                    value = 1.0f;
                AudioEndpointVolume.MasterVolumeLevelScalar = value;
                NotifyPropertyChanged();
            }
        }
        /// <inheritdoc/>
        public int Volume
        {
            get => VolumeLevelConverter.FromNativeVolume(NativeVolume);
            set
            {
                NativeVolume = VolumeLevelConverter.ToNativeVolume(value);
                NotifyPropertyChanged();
            }
        }
        /// <inheritdoc/>
        public bool Mute
        {
            get => AudioEndpointVolume.Mute;
            set
            {
                AudioEndpointVolume.Mute = value;
                NotifyPropertyChanged();
            }
        }
        #endregion Properties (IVolumeControl)
        #endregion Properties

        #region Methods
        #region Methods (EventHandlers)
        /// <summary>
        /// Triggers the <see cref="VolumeChanged"/> event.
        /// </summary>
        private void AudioEndpointVolume_OnVolumeNotification(AudioVolumeNotificationData data)
            => NotifyVolumeChanged(data);
        #endregion Methods (EventHandlers)

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    MMDevice.Dispose();
                }

                disposedValue = true;
            }
        }
        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion Methods
    }
    public static class MMDeviceExtensions
    {
        /// <summary>
        /// Gets the name of this <see cref="MMDevice"/> instance as shown in Windows.
        /// </summary>
        /// <param name="mmDevice">The <see cref="MMDevice"/> instance to retrieve the name of.</param>
        /// <returns>The actual name of <paramref name="mmDevice"/> as a <see cref="string"/>.</returns>
        public static string GetDeviceName(this MMDevice mmDevice)
            => Regex.Replace(mmDevice.DeviceFriendlyName, $"\\(\\s*?{mmDevice.DeviceFriendlyName}\\s*?\\)", "", RegexOptions.Compiled).Trim();
    }
    public class AudioDeviceManager
    {
        public AudioDeviceManager(DataFlow deviceDataFlow)
        {
            Devices = new();
            DeviceDataFlow = deviceDataFlow;

            _deviceEnumerator = new MMDeviceEnumerator(_eventContext);

            foreach (var mmDevice in _deviceEnumerator.EnumerateAudioEndPoints(DeviceDataFlow, DeviceState.Active))
            {
                CreateAndAddDeviceIfUnique(mmDevice);
            }
            Log.Debug($"Successfully initialized {Devices.Count} audio devices.");

            _deviceNotificationClient = new(_deviceEnumerator);

            _deviceNotificationClient.DeviceStateChanged += this.DeviceNotificationClient_DeviceStateChanged; ;
            _deviceNotificationClient.DeviceAdded += this.DeviceNotificationClient_DeviceAdded;
            _deviceNotificationClient.DeviceRemoved += this.DeviceNotificationClient_DeviceRemoved;
        }

        #region Events
        /// <summary>
        /// Occurs when an <see cref="AudioDevice"/> is added to the <see cref="Devices"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioDevice>? DeviceAddedToList;
        private void NotifyDeviceAddedToList(AudioDevice audioDevice) => DeviceAddedToList?.Invoke(this, audioDevice);
        /// <summary>
        /// Occurs when an <see cref="AudioDevice"/> is removed from the <see cref="Devices"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioDevice>? DeviceRemovedFromList;
        private void NotifyDeviceRemovedFromList(AudioDevice audioDevice) => DeviceRemovedFromList?.Invoke(this, audioDevice);
        #endregion Events

        #region EventHandlers (_deviceNotificationClient)
        private void DeviceNotificationClient_DeviceRemoved(object? sender, DeviceNotificationEventArgs e)
        {
            if (FindDeviceByID(e.DeviceId) is AudioDevice audioDevice)
            {
                Devices.Remove(audioDevice);

                Log.Debug($"{nameof(AudioDevice)} '{audioDevice.Name}' was removed from the system; it was removed from the list.");

                NotifyDeviceRemovedFromList(audioDevice);

                audioDevice.Dispose();
            }
        }
        private void DeviceNotificationClient_DeviceAdded(object? sender, DeviceNotificationEventArgs e)
        {
            if (!e.TryGetDevice(out MMDevice? mmDevice) || mmDevice is null) return;
            if (!DeviceDataFlow.HasFlag(mmDevice.DataFlow)) return;

            if (mmDevice.State.Equals(DeviceState.Active))
            {
                if (CreateAndAddDeviceIfUnique(mmDevice) is AudioDevice newAudioDevice)
                {
                    Log.Debug($"Detected new {nameof(AudioDevice)} '{newAudioDevice.Name}'; added it to the list.");
                }
                else
                {
                    Log.Error($"Detected new {nameof(AudioDevice)} '{mmDevice.GetDeviceName()}'; it is already in the list!");
                }
            }
            else
            {
                Log.Debug($"Detected new {nameof(AudioDevice)} '{mmDevice.GetDeviceName()}'; did not add it the list because its current state is {mmDevice.State:G}.");
            }
        }
        private void DeviceNotificationClient_DeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            if (!e.TryGetDevice(out MMDevice? mmDevice) || mmDevice is null) return;
            if (!DeviceDataFlow.HasFlag(mmDevice.DataFlow)) return;

            if (mmDevice.State == DeviceState.Active)
            {
                if (CreateAndAddDeviceIfUnique(mmDevice) is AudioDevice newAudioDevice)
                {
                    Log.Debug($"{nameof(AudioDevice)} '{newAudioDevice.Name}' state changed to {mmDevice.State:G}; added it to the list.");
                }
                else
                {
                    Log.Error($"{nameof(AudioDevice)} '{mmDevice.GetDeviceName()}' state changed to {mmDevice.State:G}; it is already in the list!");
                }
            }
            else // e.NewState != DeviceState.Active
            {
                if (FindDeviceByMMDevice(mmDevice) is AudioDevice existingAudioDevice)
                {
                    Devices.Remove(existingAudioDevice);

                    Log.Debug($"{nameof(AudioDevice)} '{existingAudioDevice.Name}' state changed to {mmDevice.State:G}; removed it from the list.");

                    NotifyDeviceRemovedFromList(existingAudioDevice);

                    existingAudioDevice.Dispose();
                }
                else
                {
                    Log.Error($"{nameof(AudioDevice)} '{mmDevice.GetDeviceName()}' state changed to {mmDevice.State:G}; cannot remove it from the list because it doesn't exist!");
                }
            }
        }
        #endregion EventHandlers (_deviceNotificationClient)

        #region Fields
        private readonly Guid _eventContext = new();
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMNotificationClient _deviceNotificationClient;
        #endregion Fields

        #region Properties
        private static VolumeControl.Log.LogWriter Log => VolumeControl.Log.FLog.Log;
        /// <summary>
        /// Gets the list of <see cref="AudioDevice"/> instances.
        /// </summary>
        public List<AudioDevice> Devices { get; }
        /// <summary>
        /// Gets the <see cref="DataFlow"/> type of the audio devices managed by this <see cref="AudioDeviceManager"/> instance.
        /// </summary>
        public DataFlow DeviceDataFlow { get; }
        #endregion Properties

        #region Methods
        #region Methods (FindDevice)
        /// <summary>
        /// Gets the <see cref="AudioDevice"/> instance associated with the given <paramref name="deviceID"/> string. (<see cref="MMDevice.ID"/>)
        /// </summary>
        /// <param name="deviceID">The ID of the target device.</param>
        /// <param name="sCompareType">The <see cref="StringComparison"/> type to use when comparing ID strings.</param>
        /// <returns>The <see cref="AudioDevice"/> associated with the given <paramref name="deviceID"/> if found; otherwise <see langword="null"/>.</returns>
        public AudioDevice? FindDeviceByID(string deviceID, StringComparison sCompareType = StringComparison.Ordinal)
            => Devices.FirstOrDefault(dev => dev.ID.Equals(deviceID, sCompareType));
        /// <summary>
        /// Gets the <see cref="AudioDevice"/> instance associated with the given <paramref name="mmDevice"/>.
        /// </summary>
        /// <param name="mmDevice">The <see cref="MMDevice"/> instance associated with the target device.</param>
        /// <returns>The <see cref="AudioDevice"/> associated with the given <paramref name="mmDevice"/> if found; otherwise <see langword="null"/>.</returns>
        public AudioDevice? FindDeviceByMMDevice(MMDevice mmDevice)
            => FindDeviceByID(mmDevice.ID);
        #endregion Methods (FindDevice)

        /// <summary>
        /// Creates a new <see cref="AudioDevice"/> from the given <paramref name="mmDevice"/> and adds it to the <see cref="Devices"/> list, and triggers the <see cref="DeviceAddedToList"/> event.
        /// </summary>
        /// <param name="mmDevice">The <see cref="MMDevice"/> instance to create a new <see cref="AudioDevice"/> from.</param>
        /// <returns>The newly-created <see cref="AudioDevice"/> instance if successful; otherwise <see langword="null"/> if <paramref name="mmDevice"/> is already associated with another device.</returns>
        private AudioDevice? CreateAndAddDeviceIfUnique(MMDevice mmDevice)
        {
            if (FindDeviceByMMDevice(mmDevice) is null)
            {
                var audioDevice = new AudioDevice(mmDevice);

                Devices.Add(audioDevice);

                NotifyDeviceAddedToList(audioDevice);

                return audioDevice;
            }
            else return null;
        }
        /// <summary>
        /// Gets the default device for the specified <paramref name="deviceRole"/>.
        /// </summary>
        /// <param name="deviceRole">The <see cref="Role"/> of the target device.</param>
        /// <returns>The default <see cref="AudioDevice"/> instance if one was found; otherwise <see langword="null"/>.</returns>
        public AudioDevice? GetDefaultDevice(Role deviceRole)
        {
            try
            {
                var defaultEndpointMMDevice = _deviceEnumerator.GetDefaultAudioEndpoint(DeviceDataFlow, deviceRole);

                return FindDeviceByID(defaultEndpointMMDevice.ID);
            }
            catch (Exception ex)
            {
                Log.Error($"Getting default audio device with role '{deviceRole:G}' failed because an exception was thrown:", ex);
                return null;
            }
        }

        #endregion Methods
    }
    /// <summary>
    /// Contains event data for the <see cref="VolumeChangedEventHandler"/> event type.
    /// </summary>
    public sealed class VolumeChangedEventArgs : EventArgs, IReadOnlyVolumeControl
    {
        #region Constructor
        /// <summary>
        /// Creates a new <see cref="VolumeChangedEventArgs"/> instance with the given <paramref name="data"/>.
        /// </summary>
        /// <param name="data">The <see cref="NAudio.CoreAudioApi.AudioVolumeNotificationData"/> object from the underlying event.</param>
        public VolumeChangedEventArgs(AudioVolumeNotificationData data)
            => AudioVolumeNotificationData = data;
        #endregion Constructor

        #region Properties
        /// <summary>
        /// The underlying <see cref="NAudio.CoreAudioApi.AudioVolumeNotificationData"/> event argument object received from NAudio.
        /// </summary>
        public AudioVolumeNotificationData AudioVolumeNotificationData { get; }
        /// <inheritdoc/>
        public float NativeVolume => AudioVolumeNotificationData.MasterVolume;
        /// <inheritdoc/>
        public int Volume => _volume ??= VolumeLevelConverter.FromNativeVolume(NativeVolume);
        private int? _volume;
        /// <inheritdoc/>
        public bool Mute => AudioVolumeNotificationData.Muted;
        #endregion Properties
    }
    /// <summary>
    /// Represents a method that is called when an audio instance's volume or mute state was changed.
    /// </summary>
    /// <param name="sender">The <see cref="AudioDevice"/> that sent the event.</param>
    /// <param name="e"></param>
    public delegate void VolumeChangedEventHandler(AudioDevice sender, VolumeChangedEventArgs e);
}