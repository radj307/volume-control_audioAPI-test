using CoreAudio;

namespace Audio
{
    /// <summary>
    /// Notifies the application about changes made to audio devices.<br/>
    /// Represents an implementation of the Core Audio API's <see cref="IMMNotificationClient"/> interface.
    /// </summary>
    public interface INotificationClient
    {
        /// <summary>
        /// Occurs when the default <see cref="Role.Multimedia"/> audio device was changed.<br/>
        /// Event arguments include the <see cref="MMDevice"/> device object.
        /// </summary>
        event EventHandler<MMDevice>? DefaultMultimediaDeviceChanged;
        /// <summary>
        /// Occurs when the default <see cref="Role.Console"/> audio device was changed.<br/>
        /// Event arguments include the <see cref="MMDevice"/> device object.
        /// </summary>
        event EventHandler<MMDevice>? DefaultConsoleDeviceChanged;
        /// <summary>
        /// Occurs when the default <see cref="Role.Communications"/> audio device was changed.<br/>
        /// Event arguments include the <see cref="MMDevice"/> device object.
        /// </summary>
        event EventHandler<MMDevice>? DefaultCommunicationsDeviceChanged;
        /// <summary>
        /// Occurs when a new audio device was added to the system.
        /// </summary>
        /// <remarks>
        /// To detect whether an audio device was <i>enabled</i>, use <see cref="DeviceStateChanged"/> instead.
        /// </remarks>
        event EventHandler<MMDevice>? DeviceAdded;
        /// <summary>
        /// Occurs when an audio device was removed from the system.<br/>
        /// Event arguments include the ID of the device that was removed.
        /// </summary>
        /// <remarks>
        /// To detect whether an audio device was <i>disabled</i>, use <see cref="DeviceStateChanged"/> instead.
        /// </remarks>
        event EventHandler<string>? DeviceRemoved;
        /// <summary>
        /// Occurs when an audio device's state was changed.<br/>
        /// Event arguments include the <see cref="MMDevice"/> device object, and the new <see cref="DeviceState"/>.
        /// </summary>
        event EventHandler<(MMDevice MMDevice, DeviceState NewState)>? DeviceStateChanged;
        /// <summary>
        /// Occurs when an audio device property value was changed.<br/>
        /// Event arguments include the <see cref="MMDevice"/> device object, and the changed <see cref="PropertyKey"/>.
        /// </summary>
        event EventHandler<(MMDevice MMDevice, PropertyKey Property)>? PropertyValueChanged;
    }
}