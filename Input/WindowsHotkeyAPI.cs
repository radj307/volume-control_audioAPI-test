using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Input;
using System.Windows.Interop;
using VolumeControl.Log;

namespace Input
{
    internal static class WindowsHotkeyAPI
    {
        private static LogWriter Log => FLog.Log;

        #region ID
        public const int MinID = 0x0000;
        public const int MaxID = 0xBFFF;
        public const int TotalUniqueIDs = MaxID - MinID;

        /// <summary>
        /// Gets the next available hotkey ID number.
        /// </summary>
        public static int NextID
        {
            get
            {
                int id = Interlocked.Increment(ref _id);
                if (id >= MaxID)
                {  // if we exceed the max ID, loop back to the min id
                    _ = Interlocked.Exchange(ref _id, MinID);
                    ++OverflowCounter;
                    Log.Error(new OverflowException($"The Hotkey ID sequencer has exceeded 0x{MaxID:X4}, and was reset to 0x{MinID:X4}. ({OverflowCounter} time{(OverflowCounter.Equals(1) ? "" : "s")})"));
                }
                return id;
            }
        }
        private static int _id = MinID;

        /// <summary>
        /// Resets the <see cref="NextID"/> counter and the <see cref="OverflowCounter"/>.
        /// This should be called whenever all hotkeys are guaranteed to have been deleted.
        /// </summary>
        public static void ResetIDs()
        {
            Interlocked.Exchange(ref _id, MinID);
            OverflowCounter = 0;
        }
        #endregion ID

        #region HasOverflown
        /// <summary>
        /// Gets the number of times that <see cref="NextID"/> has exceeded <see cref="MaxID"/> and was reset to <see cref="MinID"/>.
        /// </summary>
        public static uint OverflowCounter { get; private set; } = 0u;
        public static bool HasOverflown => OverflowCounter > 0u;
        #endregion HasOverflown

        #region WindowsAPI
        #region User32
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int UnregisterHotKey(IntPtr hWnd, int id);
        /// <summary>
        /// Possible flags to pass to the <see cref="FormatMessage(FORMAT_MESSAGE, IntPtr, int, uint, out StringBuilder, int, IntPtr)"/> method.
        /// </summary>
        [Flags]
        public enum FORMAT_MESSAGE : uint
        {
            /// <summary/>
            ALLOCATE_BUFFER = 0x00000100,
            /// <summary/>
            IGNORE_INSERTS = 0x00000200,
            /// <summary/>
            FROM_SYSTEM = 0x00001000,
            /// <summary/>
            ARGUMENT_ARRAY = 0x00002000,
            /// <summary/>
            FROM_HMODULE = 0x00000800,
            /// <summary/>
            FROM_STRING = 0x00000400
        }
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int FormatMessage(FORMAT_MESSAGE dwFlags, IntPtr lpSource, int dwMessageId, uint dwLanguageId, out StringBuilder msgOut, int nSize, IntPtr Arguments);
        #endregion User32

        #region GetLastError
        /// <summary>Gets the last error to have occurred from within the unmanaged C++ Windows API.</summary>
        /// <returns>An <see cref="int"/> representing the HResult of the last error.</returns>
        public static int GetLastError()
            => Marshal.GetLastWin32Error();
        /// <summary>
        /// Gets an error message from the given <paramref name="err"/> HResult.<br/>
        /// This is a convenience method that calls <see cref="FormatMessage(FORMAT_MESSAGE, IntPtr, int, uint, out StringBuilder, int, IntPtr)"/>.
        /// </summary>
        /// <param name="err">The HResult error ID to format into a string.</param>
        /// <returns>The string error message associated with HResult <paramref name="err"/>.</returns>
        public static string FormatError(int err)
        {
            if (err == 0)
                return "";
            _ = FormatMessage(
                FORMAT_MESSAGE.ALLOCATE_BUFFER | FORMAT_MESSAGE.FROM_SYSTEM | FORMAT_MESSAGE.IGNORE_INSERTS,
                IntPtr.Zero,
                err,
                0,
                out StringBuilder msg,
                256,
                IntPtr.Zero
            );
            return msg.ToString().Trim();
        }
        /// <summary>
        /// Gets the last error message to have occurred from within the unmanaged C++ Windows API, as a string.
        /// </summary>
        /// <returns>A string error message.</returns>
        public static string GetLastErrorString() => FormatError(GetLastError());
        /// <summary>
        /// Gets both the last HResult error ID and the associated string error message for the last error message to have occurred from within the unmanaged C++ Windows API.
        /// </summary>
        /// <returns>HResult and string error message as a tuple.</returns>
        public static (int hresult, string message) GetLastWin32Error()
        {
            int err = GetLastError();
            return (err, FormatError(err));
        }
        #endregion GetLastError

        /// <summary>Parent pointer for a message-only window.</summary>
        private static readonly IntPtr HWND_MESSAGE = new(-3);
        internal const int WM_HOTKEY = 0x312;

        /// <summary>
        /// A message-only window wrapper class that allows adding and removing hook handler methods.
        /// </summary>
        private class MessageOnlyWindow : IDisposable
        {
            internal MessageOnlyWindow() => hWndSource = new(0, 0, 0, 0, 0, 0, 0, null, HWND_MESSAGE);
            ~MessageOnlyWindow() => Dispose();

            /// <summary>The <see cref="HwndSource"/> class associated with the message-only window used for hotkeys.</summary>
            private readonly HwndSource hWndSource;

            public IntPtr Handle => hWndSource.Handle;

            /// <inheritdoc cref="HwndSource.AddHook(HwndSourceHook)"/>
            public void AddHook(HwndSourceHook hook) => hWndSource.AddHook(hook);
            /// <inheritdoc cref="HwndSource.RemoveHook(HwndSourceHook)"/>
            public void RemoveHook(HwndSourceHook hook) => hWndSource.RemoveHook(hook);

            /// <inheritdoc/>
            public void Dispose()
            {
                hWndSource.Dispose();
                GC.SuppressFinalize(this);
            }
        }
        /// <summary>
        /// The message-only window instance used for hotkeys.
        /// </summary>
        private static readonly MessageOnlyWindow messageOnlyWindow = new();

        /// <inheritdoc cref="KeyInterop.VirtualKeyFromKey(Key)"/>
        private static uint ToVirtualKey(Key key) => (uint)KeyInterop.VirtualKeyFromKey(key);

        /// <summary>
        /// Registers <paramref name="hk"/> with the windows API.
        /// </summary>
        /// <param name="hk">The <see cref="Hotkey"/> to register.</param>
        /// <returns><see langword="true"/> when successful; otherwise <see langword="false"/></returns>
        public static bool Register(Hotkey hk)
        {
            if (RegisterHotKey(messageOnlyWindow.Handle, hk.ID, (uint)hk.Modifiers, ToVirtualKey(hk.GetKey())) != 0)
            { // registration succeeded
                messageOnlyWindow.AddHook(hk.MessageHook);
                return true;
            }
            else
            { // registration failed
                (int hr, string msg) = GetLastWin32Error();
                Log.Error($"Hotkey {hk.ID} registration failed:  '{msg}' (HRESULT: {hr})");
                return false;
            }
        }
        /// <summary>
        /// Unregisters <paramref name="hk"/> with the windows API.
        /// </summary>
        /// <param name="hk">The <see cref="Hotkey"/> to unregister.</param>
        /// <returns><see langword="true"/> when successful; otherwise <see langword="false"/></returns>
        public static bool Unregister(Hotkey hk, bool reportErrors = true)
        {
            if (UnregisterHotKey(messageOnlyWindow.Handle, hk.ID) != 0)
            { // unregistration succeeded
                messageOnlyWindow.RemoveHook(hk.MessageHook);
                return true;
            }
            else
            { // unregistration failed
                if (reportErrors)
                {
                    (int hr, string msg) = GetLastWin32Error();
                    Log.Error($"Hotkey {hk.ID} unregistration failed:  '{msg}' (HRESULT: {hr})");
                }
                return false;
            }
        }
        /// <summary>
        /// Reregisters <paramref name="hk"/> with the windows API.
        /// </summary>
        /// <param name="hk">The <see cref="Hotkey"/> to reregister.</param>
        /// <returns><see langword="true"/> when registration was successful; otherwise <see langword="false"/></returns>
        public static bool Reregister(Hotkey hk)
        {
            _ = Unregister(hk);
            return Register(hk);
        }
        #endregion WindowsAPI
    }
}
