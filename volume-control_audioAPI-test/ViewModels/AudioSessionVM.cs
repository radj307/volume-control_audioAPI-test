using Audio;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using VolumeControl.Log;
using VolumeControl.TypeExtensions;
using VolumeControl.WPF;

namespace volume_control_audioAPI_test.ViewModels
{
    /// <summary>
    /// ViewModel for the <see cref="Audio.AudioSession"/> class.
    /// </summary>
    public class AudioSessionVM : INotifyPropertyChanged, IDisposable
    {
        #region Constructor
        public AudioSessionVM(AudioSession audioSession)
        {
            AudioSession = audioSession;

            Icon = GetIcon();
            Icon?.Freeze();

            AudioSession.IconPathChanged += (s, e) => Icon = GetIcon();
        }
        #endregion Constructor

        #region Properties
        public AudioSession AudioSession { get; }
        public ImageSource? Icon
        {
            get => _icon;
            set
            {
                _icon = value;
                NotifyPropertyChanged();
            }
        }
        private ImageSource? _icon;
        public AudioSessionMultiSelector? AudioSessionMultiSelector { get; set; }
        public bool? IsSelected
        {
            get => AudioSessionMultiSelector?.GetSessionSelectionState(AudioSession);
            set
            {
                if (!value.HasValue) return;
                AudioSessionMultiSelector?.SetSessionSelectionState(AudioSession, value.Value);
            }
        }
        #endregion Properties

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        #endregion Events

        #region Methods
        private ImageSource? GetIcon()
        {
            // try getting the icon from WASAPI
            var iconPath = AudioSession.AudioSessionControl.IconPath;
            if (iconPath.Length > 0 && IconExtractor.TryExtractFromPath(iconPath, out ImageSource wasapiImageSource))
            {
                return wasapiImageSource;
            }

            // try getting the icon from the process
            var proc = AudioSession.Process;

            if (proc == null) return null;
            try
            {
                if (proc.GetMainModulePath() is string path && IconExtractor.TryExtractFromPath(path, out ImageSource processImageSource))
                {
                    return processImageSource;
                }
            }
            catch (Exception ex)
            {
                FLog.Log.Error($"Failed to get an icon for session '{AudioSession.Name}' because of an exception:", ex);
            }

            return null;
        }
        public void Dispose()
        {
            ((IDisposable)this.AudioSession).Dispose();
            GC.SuppressFinalize(this);
        }
        #endregion Methods
    }
}
