﻿using Audio.Helpers;
using CoreAudio;

namespace Audio
{
    /// <summary>
    /// Manages a list of <see cref="AudioDevice"/> instances and related events.
    /// </summary>
    public sealed class AudioDeviceManager
    {
        #region Constructor
        /// <summary>
        /// Creates a new <see cref="AudioDeviceManager"/> instance.
        /// </summary>
        /// <param name="deviceDataFlow">The <see cref="DataFlow"/> type of the devices that this <see cref="AudioDeviceManager"/> instance will manage. This cannot be changed later.</param>
        /// <param name="deviceEnumerator">The <see cref="MMDeviceEnumerator"/> instance to use.</param>
        public AudioDeviceManager(DataFlow deviceDataFlow, MMDeviceEnumerator deviceEnumerator)
        {
            DeviceDataFlow = deviceDataFlow;
            _devices = new();
            // initialize core audio api objects
            _eventContext = deviceEnumerator.eventContext;
            _deviceEnumerator = deviceEnumerator;
            _deviceNotificationClient = new(_deviceEnumerator);
            // connect events
            _deviceNotificationClient.DeviceStateChanged += this.DeviceNotificationClient_DeviceStateChanged;
            _deviceNotificationClient.DeviceAdded += this.DeviceNotificationClient_DeviceAdded;
            _deviceNotificationClient.DeviceRemoved += this.DeviceNotificationClient_DeviceRemoved;
            // initialize devices
            foreach (var mmDevice in _deviceEnumerator.EnumerateAudioEndPoints(DeviceDataFlow, DeviceState.Active))
            {
                CreateAndAddDeviceIfUnique(mmDevice);
            }
            Log.Debug($"Successfully initialized {Devices.Count} {nameof(AudioDevice)}s.");
        }
        /// <summary>
        /// Creates a new <see cref="AudioDeviceManager"/> instance.
        /// </summary>
        /// <param name="deviceDataFlow">The <see cref="DataFlow"/> type of the devices that this <see cref="AudioDeviceManager"/> instance will manage. This cannot be changed later.</param>
        public AudioDeviceManager(DataFlow deviceDataFlow) : this(deviceDataFlow, new MMDeviceEnumerator(new Guid())) { }
        #endregion Constructor

        #region Events
        /// <summary>
        /// Occurs when an <see cref="AudioDevice"/> was added to the <see cref="Devices"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioDevice>? DeviceAddedToList;
        private void NotifyDeviceAddedToList(AudioDevice audioDevice) => DeviceAddedToList?.Invoke(this, audioDevice);
        /// <summary>
        /// Occurs when an <see cref="AudioDevice"/> was removed from the <see cref="Devices"/> list for any reason.
        /// </summary>
        public event EventHandler<AudioDevice>? DeviceRemovedFromList;
        private void NotifyDeviceRemovedFromList(AudioDevice audioDevice) => DeviceRemovedFromList?.Invoke(this, audioDevice);
        /// <summary>
        /// Occurs when an <see cref="AudioDevice"/> instance's state was changed.
        /// </summary>
        public event EventHandler<AudioDevice>? DeviceStateChanged;
        private void NotifyDeviceStateChanged(AudioDevice audioDevice) => DeviceStateChanged?.Invoke(this, audioDevice);
        #endregion Events

        #region Fields
        private readonly Guid _eventContext;
        private readonly MMDeviceEnumerator _deviceEnumerator;
        private readonly MMNotificationClient _deviceNotificationClient;
        #endregion Fields

        #region Properties
        private static VolumeControl.Log.LogWriter Log => VolumeControl.Log.FLog.Log;
        /// <summary>
        /// Gets the list of <see cref="AudioDevice"/> instances.
        /// </summary>
        public IReadOnlyList<AudioDevice> Devices => _devices;
        /// <summary>
        /// The underlying <see cref="List{T}"/> for the <see cref="Devices"/> property.
        /// </summary>
        private readonly List<AudioDevice> _devices;
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
        /// <param name="comparisonType">The <see cref="StringComparison"/> type to use when comparing ID strings.</param>
        /// <returns>The <see cref="AudioDevice"/> associated with the given <paramref name="deviceID"/> if found; otherwise <see langword="null"/>.</returns>
        public AudioDevice? FindDeviceByID(string deviceID, StringComparison comparisonType = StringComparison.Ordinal)
            => Devices.FirstOrDefault(dev => dev.ID.Equals(deviceID, comparisonType));
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

                _devices.Add(audioDevice);

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

        #region Methods (_deviceNotificationClient EventHandlers)
        private void DeviceNotificationClient_DeviceStateChanged(object? sender, DeviceStateChangedEventArgs e)
        {
            if (!e.TryGetDevice(out MMDevice? mmDevice) || mmDevice is null) return;
            if (!DeviceDataFlow.HasFlag(mmDevice.DataFlow)) return;

            if (e.DeviceState == DeviceState.Active && mmDevice.State == DeviceState.Active)
            {
                if (CreateAndAddDeviceIfUnique(mmDevice) is AudioDevice newAudioDevice)
                {
                    NotifyDeviceStateChanged(newAudioDevice);
                    Log.Debug($"{nameof(AudioDevice)} '{newAudioDevice.Name}' state changed to {mmDevice.State:G}; added it to the list.");
                }
                else
                {
                    NotifyDeviceStateChanged(FindDeviceByMMDevice(mmDevice)!);
                    Log.Error($"{nameof(AudioDevice)} '{mmDevice.GetDeviceName()}' state changed to {mmDevice.State:G}; it is already in the list!");
                }
            }
            else // e.DeviceState != DeviceState.Active
            {
                if (FindDeviceByMMDevice(mmDevice) is AudioDevice existingAudioDevice)
                {
                    NotifyDeviceStateChanged(existingAudioDevice);

                    _devices.Remove(existingAudioDevice);

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
        private void DeviceNotificationClient_DeviceRemoved(object? sender, DeviceNotificationEventArgs e)
        {
            if (FindDeviceByID(e.DeviceId) is AudioDevice audioDevice)
            {
                _devices.Remove(audioDevice);

                Log.Debug($"{nameof(AudioDevice)} '{audioDevice.Name}' was removed from the system; it was removed from the list.");

                NotifyDeviceRemovedFromList(audioDevice);

                audioDevice.Dispose();
            }
        }
        #endregion Methods (_deviceNotificationClient EventHandlers)
        #endregion Methods
    }
}