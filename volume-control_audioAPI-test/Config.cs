using Newtonsoft.Json;
using System.ComponentModel;
using VolumeControl.Log;

namespace volume_control_audioAPI_test
{
    /// <summary>
    /// Contains the application configuration and logic to read from and write to JSON files.
    /// </summary>
    [JsonObject]
    public class Config : AppConfig.ConfigurationFile
    {
        #region Constructor
        /// <summary>
        /// Creates a new <see cref="Config"/> instance.
        /// </summary>
        /// <remarks>The first time this is called, the <see cref="AppConfig.Configuration.Default"/> property is set to that instance; all subsequent calls do not update this property.</remarks>
        public Config() : base(_filePath) => this.ResumeAutoSave();
        #endregion Constructor

        #region Methods
        /// <summary>
        /// Resumes automatic saving of the config to disk whenever a <see cref="PropertyChanged"/> event is triggered.
        /// </summary>
        public void ResumeAutoSave() => PropertyChanged += this.HandlePropertyChanged;
        /// <summary>
        /// Pauses automatic saving of the config to disk whenever a <see cref="PropertyChanged"/> event is triggered.
        /// </summary>
        public void PauseAutoSave() => PropertyChanged -= this.HandlePropertyChanged;
        #endregion Methods

        #region EventHandlers
        private void HandlePropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            Log.Debug($"Property '{e.PropertyName}' was modified, saving {nameof(Config)}...");
            this.Save();
        }
        #endregion EventHandlers

        #region Statics
        private static LogWriter Log => FLog.Log;
        // Default filepath used for the config file:
        private const string _filePath = "VolumeControl.json";
        #endregion Statics

        #region Log
        /// <summary>
        /// Gets or sets whether the log is enabled or not.<br/>
        /// See <see cref="Log.SettingsInterface.EnableLogging"/>
        /// </summary>
        /// <remarks><b>Default: <see langword="true"/></b></remarks>
        public bool EnableLogging { get; set; } = true;
        /// <summary>
        /// Gets or sets the location of the log file.<br/>
        /// See <see cref="Log.SettingsInterface.LogPath"/>
        /// </summary>
        /// <remarks><b>Default: "VolumeControl.log"</b></remarks>
        public string LogPath { get; set; } = "VolumeControl.log";
        /// <summary>
        /// Gets or sets the <see cref="Log.Enum.EventType"/> filter used for messages.<br/>
        /// See <see cref="Log.SettingsInterface.LogFilter"/>
        /// </summary>
        /// <remarks><b>Default: <see cref="Log.Enum.EventType.ALL_EXCEPT_DEBUG"/></b></remarks>
        public VolumeControl.Log.Enum.EventType LogFilter { get; set; } = VolumeControl.Log.Enum.EventType.ALL_EXCEPT_DEBUG;
        /// <summary>
        /// Gets or sets whether the log is cleared when the program starts.<br/>
        /// See <see cref="Log.SettingsInterface.LogClearOnInitialize"/>
        /// </summary>
        /// <remarks><b>Default: <see langword="true"/></b></remarks>
        public bool LogClearOnInitialize { get; set; } = true;
        /// <summary>
        /// Gets or sets the format string used for timestamps in the log.<br/>
        /// See <see cref="Log.SettingsInterface.LogTimestampFormat"/>
        /// </summary>
        /// <remarks><b>Default: "HH:mm:ss:fff"</b></remarks>
        public string LogTimestampFormat { get; set; } = "HH:mm:ss:fff";
        /// <summary>
        /// 
        /// See <see cref="Log.SettingsInterface.LogEnableStackTrace"/>
        /// </summary>
        /// <remarks><b>Default: <see langword="true"/></b></remarks>
        public bool LogEnableStackTrace { get; set; } = true;
        /// <summary>
        /// 
        /// See <see cref="Log.SettingsInterface.LogEnableStackTraceLineCount"/>
        /// </summary>
        /// <remarks><b>Default: <see langword="true"/></b></remarks>
        public bool LogEnableStackTraceLineCount { get; set; } = true;
        #endregion Log
    }
}
