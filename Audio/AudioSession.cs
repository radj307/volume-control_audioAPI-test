using Audio.Events;
using CoreAudio;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using VolumeControl.Log;

namespace Audio
{
    public class AudioSession : IVolumeControl, IReadOnlyVolumeControl, IVolumePeakMeter, INotifyPropertyChanged, IDisposable
    {
        public AudioSession(AudioSessionControl2 audioSessionControl2)
        {
            AudioSessionControl = audioSessionControl2;

            PID = AudioSessionControl.ProcessID;

            AudioSessionControl.OnDisplayNameChanged += this.AudioSessionControl_OnDisplayNameChanged;
            AudioSessionControl.OnIconPathChanged += this.AudioSessionControl_OnIconPathChanged;
            AudioSessionControl.OnSessionDisconnected += this.AudioSessionControl_OnSessionDisconnected;
            AudioSessionControl.OnSimpleVolumeChanged += this.AudioSessionControl_OnSimpleVolumeChanged;
            AudioSessionControl.OnStateChanged += this.AudioSessionControl_OnStateChanged;

            ProcessName = Process?.ProcessName ?? string.Empty;
        }

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        /// <summary>
        /// Occurs when this <see cref="AudioSession"/> instance has been disconnected.
        /// </summary>
        public event AudioSessionControl2.SessionDisconnectedDelegate? SessionDisconnected;
        private void NotifySessionDisconnected(AudioSessionDisconnectReason disconnectReason) => SessionDisconnected?.Invoke(this, disconnectReason);
        /// <summary>
        /// Occurs when the state of this <see cref="AudioSession"/> instance was changed.
        /// </summary>
        public event EventHandler<AudioSessionState>? StateChanged;
        private void NotifyStateChanged(AudioSessionState newState) => StateChanged?.Invoke(this, newState);
        /// <summary>
        /// Occurs when the display name of this <see cref="AudioSession"/> instance has changed.
        /// </summary>
        public event EventHandler<string>? DisplayNameChanged;
        private void NotifyDisplayNameChanged(string newDisplayName) => DisplayNameChanged?.Invoke(this, newDisplayName);
        /// <summary>
        /// Occurs when the display icon path for this <see cref="AudioSession"/> instance has changed.
        /// </summary>
        public event EventHandler<string>? IconPathChanged;
        private void NotifyIconPathChanged(string newIconPath) => IconPathChanged?.Invoke(this, newIconPath);
        /// <summary>
        /// Occurs when the volume level or mute state of this <see cref="AudioSession"/> has changed.
        /// </summary>
        public event VolumeChangedEventHandler? VolumeChanged;
        private void NotifyVolumeChanged(float newVolume, bool newMute) => VolumeChanged?.Invoke(this, new(newVolume, newMute));
        #endregion Events

        #region Fields
        /// <summary>
        /// Used to prevent duplicate <see cref="PropertyChanged"/> events from being fired.
        /// </summary>
        private bool isNotifying = false;
        #endregion Fields

        #region Properties
        private static LogWriter Log => FLog.Log;
        public AudioSessionControl2 AudioSessionControl { get; }
        internal SimpleAudioVolume SimpleAudioVolume => AudioSessionControl.SimpleAudioVolume;

        /// <summary>
        /// Gets the process ID of the process associated with this <see cref="AudioSession"/> instance.
        /// </summary>
        public uint PID { get; }
        public string ProcessName { get; }
        public Process? Process
        {
            get
            {
                try
                {
                    return Process.GetProcessById(Convert.ToInt32(PID));

                }
                catch (Exception ex)
                {
                    Log.Error($"Failed to get process with ID '{PID}' because of an exception:", ex);
                    return null;
                }
            }
        }

        #region Properties (IVolumeControl)
        public float NativeVolume
        {
            get => SimpleAudioVolume.MasterVolume;
            set
            {
                if (value < 0.0f)
                    value = 0.0f;
                else if (value > 1.0f)
                    value = 1.0f;

                SimpleAudioVolume.MasterVolume = value;
                if (isNotifying) return; //< don't duplicate propertychanged notifications
                isNotifying = true;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(Volume));
                isNotifying = false;
            }
        }
        public int Volume
        {
            get => VolumeLevelConverter.FromNativeVolume(NativeVolume);
            set
            {
                NativeVolume = VolumeLevelConverter.ToNativeVolume(value);
                if (isNotifying) return; //< don't duplicate propertychanged notifications
                isNotifying = true;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(NativeVolume));
                isNotifying = false;
            }
        }
        public bool Mute
        {
            get => SimpleAudioVolume.Mute;
            set
            {
                SimpleAudioVolume.Mute = value;
                NotifyPropertyChanged();
            }
        }
        #endregion Properties (IVolumeControl)
        #region Properties (IVolumePeakMeter)
        public float PeakMeterValue
            => AudioSessionControl.AudioMeterInformation.MasterPeakValue;
        #endregion Properties (IVolumePeakMeter)
        #endregion Properties

        #region Methods
        #region Methods (EventHandlers)
        /// <summary>
        /// Triggers the <see cref="DisplayNameChanged"/> event.
        /// </summary>
        private void AudioSessionControl_OnDisplayNameChanged(object sender, string newDisplayName)
            => NotifyDisplayNameChanged(newDisplayName);
        /// <summary>
        /// Triggers the <see cref="IconPathChanged"/> event.
        /// </summary>
        private void AudioSessionControl_OnIconPathChanged(object sender, string newIconPath)
            => NotifyIconPathChanged(newIconPath);
        /// <summary>
        /// Triggers the <see cref="SessionDisconnected"/> event.
        /// </summary>
        private void AudioSessionControl_OnSessionDisconnected(object sender, AudioSessionDisconnectReason disconnectReason)
            => NotifySessionDisconnected(disconnectReason);
        /// <summary>
        /// Triggers the <see cref="VolumeChanged"/> event.
        /// </summary>
        private void AudioSessionControl_OnSimpleVolumeChanged(object sender, float newVolume, bool newMute)
            => NotifyVolumeChanged(newVolume, newMute);
        /// <summary>
        /// Triggers the <see cref="StateChanged"/> event.
        /// </summary>
        private void AudioSessionControl_OnStateChanged(object sender, AudioSessionState newState)
            => NotifyStateChanged(newState);
        #endregion Methods (EventHandlers)

        public void Dispose()
        {
            ((IDisposable)this.AudioSessionControl).Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Methods
    }
}