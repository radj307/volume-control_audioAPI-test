using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace Input
{
    /// <summary>
    /// A single hotkey instance.
    /// </summary>
    public sealed class Hotkey : INotifyPropertyChanged, IDisposable
    {
        public Hotkey(Key key, EModifier modifiers, bool registered)
        {
            ID = WindowsHotkeyAPI.NextID;
            _key = key;
            _modifiers = modifiers;
            _registered = registered;

            WindowsHotkeyAPI.Unregister(this, reportErrors: false);
            if (Registered)
            {
                WindowsHotkeyAPI.Register(this);
            }
        }

        #region Events
        public event PropertyChangedEventHandler? PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "") => PropertyChanged?.Invoke(this, new(propertyName));
        /// <summary>
        /// Occurs when this hotkey combination was pressed by the user.
        /// </summary>
        public event EventHandler? Pressed;
        private void NotifyPressed() => Pressed?.Invoke(this, new());
        #endregion Events

        #region Properties
        /// <summary>
        /// Gets the unique ID number associated with this <see cref="Hotkey"/> instance.
        /// </summary>
        public int ID { get; }
        /// <summary>
        /// Gets or sets the primary <see cref="System.Windows.Input.Key"/> of this <see cref="Hotkey"/> instance.
        /// </summary>
        public Key Key
        {
            get => _key;
            set
            {
                _key = value;
                WindowsHotkeyAPI.Reregister(this);
                NotifyPropertyChanged();
            }
        }
        private Key _key;
        /// <summary>
        /// Gets or sets the <see cref="EModifier"/> keys of this <see cref="Hotkey"/> instance.
        /// </summary>
        public EModifier Modifiers
        {
            get => _modifiers;
            set
            {
                var changes = _modifiers ^ value;
                _modifiers = value;
                WindowsHotkeyAPI.Reregister(this);
                NotifyPropertyChanged();
                // Notify property changed for modifier properties:
                if (changes.Equals(0)) return;
                if (changes.HasFlag(EModifier.Shift))
                {
                    NotifyPropertyChanged(nameof(Shift));
                }
                if (changes.HasFlag(EModifier.Alt))
                {
                    NotifyPropertyChanged(nameof(Alt));
                }
                if (changes.HasFlag(EModifier.Ctrl))
                {
                    NotifyPropertyChanged(nameof(Ctrl));
                }
                if (changes.HasFlag(EModifier.Super))
                {
                    NotifyPropertyChanged(nameof(Super));
                }
            }
        }
        private EModifier _modifiers;
        /// <summary>
        /// Gets or sets whether the <see cref="EModifier.Shift"/> modifier key is enabled.
        /// </summary>
        public bool Shift
        {
            get => Modifiers.HasFlag(EModifier.Shift);
            set
            {
                if (value)
                { // add Shift
                    Modifiers |= EModifier.Shift;
                }
                else
                { // remove Shift
                    Modifiers &= ~EModifier.Shift;
                }
                //< don't notify property changed because Modifiers handles that for us
            }
        }
        /// <summary>
        /// Gets or sets whether the <see cref="EModifier.Alt"/> modifier key is enabled.
        /// </summary>
        public bool Alt
        {
            get => Modifiers.HasFlag(EModifier.Alt);
            set
            {
                if (value)
                { // add Alt
                    Modifiers |= EModifier.Alt;
                }
                else
                { // remove Alt
                    Modifiers &= ~EModifier.Alt;
                }
                //< don't notify property changed because Modifiers handles that for us
            }
        }
        /// <summary>
        /// Gets or sets whether the <see cref="EModifier.Ctrl"/> modifier key is enabled.
        /// </summary>
        public bool Ctrl
        {
            get => Modifiers.HasFlag(EModifier.Ctrl);
            set
            {
                if (value)
                { // add Ctrl
                    Modifiers |= EModifier.Ctrl;
                }
                else
                { // remove Ctrl
                    Modifiers &= ~EModifier.Ctrl;
                }
                //< don't notify property changed because Modifiers handles that for us
            }
        }
        /// <summary>
        /// Gets or sets whether the <see cref="EModifier.Super"/> modifier key is enabled.
        /// </summary>
        public bool Super
        {
            get => Modifiers.HasFlag(EModifier.Super);
            set
            {
                if (value)
                { // add Super
                    Modifiers |= EModifier.Super;
                }
                else
                { // remove Super
                    Modifiers &= ~EModifier.Super;
                }
                //< don't notify property changed because Modifiers handles that for us
            }
        }
        /// <summary>
        /// Gets or sets the registration state of this <see cref="Hotkey"/> instance.
        /// </summary>
        public bool Registered
        {
            get => _registered;
            set
            {
                if (value)
                { // register
                    _registered = WindowsHotkeyAPI.Register(this);
                }
                else
                { // unregister
                    _registered = !WindowsHotkeyAPI.Unregister(this);
                }
                NotifyPropertyChanged();
            }
        }
        private bool _registered;
        /// <summary>
        /// Gets the message hook method for this <see cref="Hotkey"/> instance.
        /// </summary>
        internal HwndSourceHook MessageHook => MessageHookImpl;
        #endregion Properties

        #region Methods
        /// <summary>
        /// Receives window events and handles <see cref="WindowsHotkeyAPI.WM_HOTKEY"/> events that apply to this <see cref="Hotkey"/> instance.
        /// </summary>
        private IntPtr MessageHookImpl(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg.Equals(WindowsHotkeyAPI.WM_HOTKEY) && wParam.ToInt32().Equals(ID))
            { // msg type is WM_HOTKEY and the hotkey's ID matches this instance
                handled = true;
                NotifyPressed();
            }
            return IntPtr.Zero;
        }

        public void Dispose() => WindowsHotkeyAPI.Unregister(this, reportErrors: false);
        #endregion Methods
    }
}
