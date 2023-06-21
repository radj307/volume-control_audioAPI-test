namespace Input
{
    public class HotkeyManager
    {
        public HotkeyManager(IEnumerable<HotkeyAction> hotkeyActions)
        {
            Hotkeys = new();
            HotkeyActions = hotkeyActions.ToList();
        }

        public List<Hotkey> Hotkeys { get; }
        public List<HotkeyAction> HotkeyActions { get; }
    }
}
