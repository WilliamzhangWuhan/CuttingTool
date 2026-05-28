using System;
using System.Runtime.InteropServices;

namespace ScreenshotPin
{
    internal static class NativeMethods
    {
        public const int WmClipboardUpdate = 0x031D;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
    }
}
