using System;
using System.Runtime.InteropServices;

namespace ScreenshotPin
{
    internal static class NativeMethods
    {
        public const int WmKeyDown = 0x0100;
        public const int WmSysKeyDown = 0x0104;
        public const int WmClipboardUpdate = 0x031D;
        public const int WhKeyboardLl = 13;
        public const int VkS = 0x53;
        public const int VkShift = 0x10;
        public const int VkSnapshot = 0x2C;
        public const int VkLeftWin = 0x5B;
        public const int VkRightWin = 0x5C;

        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool AddClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint RegisterClipboardFormat(string format);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
