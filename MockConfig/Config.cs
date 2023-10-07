using System.ComponentModel;
using WPF;

namespace MockConfig
{
    public class Config : INotifyPropertyChanged
    {
        #region Mock
        static Config()
        {
            Default = new();
        }
        public static Config Default { get; }
        public event PropertyChangedEventHandler? PropertyChanged;
        #endregion Mock

        public string TargetDeviceID { get; set; } = string.Empty;
        public TargetInfo TargetSession { get; set; } = TargetInfo.Empty;
        public TargetInfo[] TargetSessions { get; set; } = Array.Empty<TargetInfo>();
        public ObservableImmutableList<string> HiddenSessionProcessNames { get; set; } = new();
    }
}