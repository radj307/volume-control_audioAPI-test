using System.Collections;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Media;
using VolumeControl.Core.Input.Actions;
using VolumeControl.Log;

namespace Input
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class HotkeyActionGroupAttribute : Attribute
    {
        public HotkeyActionGroupAttribute([CallerMemberName] string groupName = "")
            => GroupName = groupName;

        public string GroupName { get; set; }
        public string? GroupColor { get; set; } = null;
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class HotkeyActionAttribute : Attribute
    {
        public HotkeyActionAttribute([CallerMemberName] string displayName = "")
            => DisplayName = displayName;

        public string DisplayName { get; set; }
        public string? Description { get; set; } = null;
        public string GroupName { get; set; } = string.Empty;
        public string? GroupColor { get; set; } = null;
        public bool InsertSpacesInName { get; set; } = true;

        private string GetNameString() => InsertSpacesInName ? Regex.Replace(DisplayName, "\\B([A-Z])", " $1") : DisplayName;

        /// <summary>
        /// Creates a new <see cref="HotkeyActionData"/> instance representing the final, interpolated 
        /// </summary>
        /// <returns>A new <see cref="HotkeyActionData"/> instance representing this hotkey action.</returns>
        public HotkeyActionData GetActionData() => new(GetNameString(), Description, GroupName, GroupColor is null ? null : new SolidColorBrush((Color)ColorConverter.ConvertFromString(GroupColor)));
    }
    public interface IHotkeyActionSetting
    {
        string SettingName { get; }
        Type SettingType { get; }
        string? Description { get; }
    }
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HotkeyActionSettingAttribute : Attribute, IHotkeyActionSetting
    {
        public HotkeyActionSettingAttribute(string settingName, Type settingType)
        {
            SettingName = settingName;
            SettingType = settingType;
        }

        public string SettingName { get; set; }
        public Type SettingType { get; set; }
        public string? Description { get; set; } = null;
    }
    public class HotkeyActionSetting : IHotkeyActionSetting, INotifyPropertyChanged
    {
        public HotkeyActionSetting(HotkeyActionSettingAttribute hotkeyActionSettingAttribute)
        {
            SettingName = hotkeyActionSettingAttribute.SettingName;
            SettingType = hotkeyActionSettingAttribute.SettingType;
            Description = hotkeyActionSettingAttribute.Description;
        }

        public string SettingName { get; }
        public Type SettingType { get; }
        public string? Description { get; }
        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                NotifyPropertyChanged();
            }
        }
        private object? _value;

        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
    }
    public class HotkeyActionSettingContainer : IList<HotkeyActionSetting>, IList, IReadOnlyList<HotkeyActionSetting>
    {
        public HotkeyActionSettingContainer(params HotkeyActionSetting[] hotkeyActionSettings)
        {
            Items = hotkeyActionSettings.ToList();
        }

        public List<HotkeyActionSetting> Items { get; } = new();

        public int Count => ((ICollection<HotkeyActionSetting>)this.Items).Count;

        public bool IsReadOnly => ((ICollection<HotkeyActionSetting>)this.Items).IsReadOnly;

        public bool IsFixedSize => ((IList)this.Items).IsFixedSize;

        public bool IsSynchronized => ((ICollection)this.Items).IsSynchronized;

        public object SyncRoot => ((ICollection)this.Items).SyncRoot;

        object? IList.this[int index] { get => ((IList)this.Items)[index]; set => ((IList)this.Items)[index] = value; }
        public HotkeyActionSetting this[int index] { get => ((IList<HotkeyActionSetting>)this.Items)[index]; set => ((IList<HotkeyActionSetting>)this.Items)[index] = value; }
        public HotkeyActionSetting this[string settingName, StringComparison stringComparisonType = StringComparison.Ordinal]
        {
            get => Items.First(it => it.SettingName.Equals(settingName, stringComparisonType));
        }

        public int IndexOf(HotkeyActionSetting item) => ((IList<HotkeyActionSetting>)this.Items).IndexOf(item);
        public void Insert(int index, HotkeyActionSetting item) => ((IList<HotkeyActionSetting>)this.Items).Insert(index, item);
        public void RemoveAt(int index) => ((IList<HotkeyActionSetting>)this.Items).RemoveAt(index);
        public void Add(HotkeyActionSetting item) => ((ICollection<HotkeyActionSetting>)this.Items).Add(item);
        public void Clear() => ((ICollection<HotkeyActionSetting>)this.Items).Clear();
        public bool Contains(HotkeyActionSetting item) => ((ICollection<HotkeyActionSetting>)this.Items).Contains(item);
        public void CopyTo(HotkeyActionSetting[] array, int arrayIndex) => ((ICollection<HotkeyActionSetting>)this.Items).CopyTo(array, arrayIndex);
        public bool Remove(HotkeyActionSetting item) => ((ICollection<HotkeyActionSetting>)this.Items).Remove(item);
        public IEnumerator<HotkeyActionSetting> GetEnumerator() => ((IEnumerable<HotkeyActionSetting>)this.Items).GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => ((System.Collections.IEnumerable)this.Items).GetEnumerator();
        public int Add(object? value) => ((IList)this.Items).Add(value);
        public bool Contains(object? value) => ((IList)this.Items).Contains(value);
        public int IndexOf(object? value) => ((IList)this.Items).IndexOf(value);
        public void Insert(int index, object? value) => ((IList)this.Items).Insert(index, value);
        public void Remove(object? value) => ((IList)this.Items).Remove(value);
        public void CopyTo(Array array, int index) => ((ICollection)this.Items).CopyTo(array, index);
    }
    /// <summary>
    /// Represents a function that is called by a hotkey press.
    /// </summary>
    public class HotkeyAction
    {
        public HotkeyAction(object? objectInstance, MethodInfo methodInfo, HotkeyActionData hotkeyActionData)
        {
            ObjectInstance = objectInstance;
            MethodInfo = methodInfo;
            ActionName = hotkeyActionData.ActionName;
            ActionDescription = hotkeyActionData.ActionDescription;
        }

        #region Properties
        private static LogWriter Log => FLog.Log;
        public object? ObjectInstance { get; }
        public MethodInfo MethodInfo { get; }
        public string ActionName { get; }
        public string? ActionDescription { get; }
        #endregion Properties

        public void Invoke(params object?[] parameters)
        {
            try
            {
                MethodInfo.Invoke(ObjectInstance, parameters);
            }
            catch (Exception ex)
            {
                Log.Error($"An exception occurred when invoking {nameof(HotkeyAction)} '{ActionName}':", ex);
            }
        }
        public HotkeyActionSettingContainer GetDefaultHotkeyActionSettings()
        {
            HotkeyActionSettingContainer container = new();

            foreach (var settingAttribute in MethodInfo.GetCustomAttributes<HotkeyActionSettingAttribute>())
            {
                container.Add(new HotkeyActionSetting(settingAttribute));
            }

            return container;
        }
    }

    public sealed class HotkeyPressedEventArgs
    {
        public HotkeyPressedEventArgs(HotkeyActionSettingContainer hotkeyActionSettings)
        {
            ActionSettings = hotkeyActionSettings;
        }

        public HotkeyActionSettingContainer ActionSettings { get; }
    }
}
