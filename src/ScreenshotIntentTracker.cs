using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScreenshotPin
{
    internal sealed class ScreenshotIntentTracker : IDisposable
    {
        private static readonly TimeSpan ScreenshotWindow = TimeSpan.FromSeconds(45);

        private NativeMethods.LowLevelKeyboardProc _hookCallback;
        private IntPtr _hookHandle;
        private DateTime _lastScreenshotRequestUtc = DateTime.MinValue;
        private bool _disposed;

        public void Start()
        {
            if (_hookHandle != IntPtr.Zero)
            {
                return;
            }

            _hookCallback = OnKeyboardHook;
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule currentModule = currentProcess.MainModule)
            {
                IntPtr moduleHandle = NativeMethods.GetModuleHandle(currentModule.ModuleName);
                _hookHandle = NativeMethods.SetWindowsHookEx(
                    NativeMethods.WhKeyboardLl,
                    _hookCallback,
                    moduleHandle,
                    0);
            }
        }

        public bool HasPendingScreenshotRequest()
        {
            if (_lastScreenshotRequestUtc == DateTime.MinValue)
            {
                return false;
            }

            if (DateTime.UtcNow - _lastScreenshotRequestUtc > ScreenshotWindow)
            {
                ClearPendingScreenshotRequest();
                return false;
            }

            return true;
        }

        public void ClearPendingScreenshotRequest()
        {
            _lastScreenshotRequestUtc = DateTime.MinValue;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            if (_hookHandle != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }
        }

        private IntPtr OnKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && IsKeyDownMessage(wParam))
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (IsScreenshotShortcut(vkCode))
                {
                    _lastScreenshotRequestUtc = DateTime.UtcNow;
                }
            }

            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private static bool IsKeyDownMessage(IntPtr wParam)
        {
            int message = wParam.ToInt32();
            return message == NativeMethods.WmKeyDown || message == NativeMethods.WmSysKeyDown;
        }

        private static bool IsScreenshotShortcut(int vkCode)
        {
            if (vkCode == NativeMethods.VkSnapshot)
            {
                return true;
            }

            return vkCode == NativeMethods.VkS
                && IsKeyPressed(NativeMethods.VkShift)
                && (IsKeyPressed(NativeMethods.VkLeftWin) || IsKeyPressed(NativeMethods.VkRightWin));
        }

        private static bool IsKeyPressed(int virtualKey)
        {
            return (NativeMethods.GetAsyncKeyState(virtualKey) & 0x8000) != 0;
        }
    }
}
