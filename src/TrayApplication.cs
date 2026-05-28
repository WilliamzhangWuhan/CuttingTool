using System;
using System.Windows;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace ScreenshotPin
{
    internal sealed class TrayApplication : IDisposable
    {
        private readonly Application _application;
        private readonly CaptureManager _captureManager;
        private ClipboardWatcher _clipboardWatcher;
        private Forms.NotifyIcon _notifyIcon;
        private Forms.ToolStripMenuItem _autoPinItem;
        private Forms.ToolStripMenuItem _topmostItem;
        private bool _disposed;

        public TrayApplication(Application application)
        {
            _application = application ?? throw new ArgumentNullException("application");
            _captureManager = new CaptureManager();
        }

        public void Start()
        {
            CreateTrayIcon();

            _clipboardWatcher = new ClipboardWatcher(_application.Dispatcher);
            _clipboardWatcher.ImageAvailable += OnClipboardImageAvailable;
            _clipboardWatcher.Start();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            if (_clipboardWatcher != null)
            {
                _clipboardWatcher.Dispose();
                _clipboardWatcher = null;
            }

            _captureManager.Dispose();

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
                _notifyIcon = null;
            }
        }

        private void CreateTrayIcon()
        {
            _autoPinItem = new Forms.ToolStripMenuItem("自动锚定新截图")
            {
                Checked = true,
                CheckOnClick = true
            };
            _autoPinItem.CheckedChanged += delegate
            {
                _captureManager.AutoPinEnabled = _autoPinItem.Checked;
            };

            _topmostItem = new Forms.ToolStripMenuItem("锚定窗口置于最上层")
            {
                Checked = true,
                CheckOnClick = true
            };
            _topmostItem.CheckedChanged += delegate
            {
                _captureManager.PinsTopmost = _topmostItem.Checked;
            };

            var pinCurrentItem = new Forms.ToolStripMenuItem("锚定当前剪贴板图片");
            pinCurrentItem.Click += delegate { Dispatch(_captureManager.PinCurrentClipboardImage); };

            var restoreMinimizedItem = new Forms.ToolStripMenuItem("恢复最小化窗口");
            restoreMinimizedItem.Click += delegate { Dispatch(_captureManager.RestoreMinimizedWindows); };

            var closeAllItem = new Forms.ToolStripMenuItem("关闭所有锚定窗口");
            closeAllItem.Click += delegate { Dispatch(_captureManager.CloseAll); };

            var exitItem = new Forms.ToolStripMenuItem("退出");
            exitItem.Click += delegate { Dispatch(_application.Shutdown); };

            var menu = new Forms.ContextMenuStrip();
            menu.Items.Add(_autoPinItem);
            menu.Items.Add(_topmostItem);
            menu.Items.Add(pinCurrentItem);
            menu.Items.Add(restoreMinimizedItem);
            menu.Items.Add(closeAllItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            _notifyIcon = new Forms.NotifyIcon
            {
                Icon = LoadTrayIcon(),
                Text = "Screenshot Pin",
                ContextMenuStrip = menu,
                Visible = true
            };

            _notifyIcon.DoubleClick += delegate { Dispatch(_captureManager.PinCurrentClipboardImage); };
        }

        private void OnClipboardImageAvailable(object sender, System.Windows.Media.Imaging.BitmapSource image)
        {
            _captureManager.HandleClipboardImage(image);
        }

        private void Dispatch(Action action)
        {
            if (action == null)
            {
                return;
            }

            _application.Dispatcher.BeginInvoke(action);
        }

        private static Drawing.Icon LoadTrayIcon()
        {
            try
            {
                return Drawing.Icon.ExtractAssociatedIcon(Forms.Application.ExecutablePath)
                    ?? Drawing.SystemIcons.Application;
            }
            catch
            {
                return Drawing.SystemIcons.Application;
            }
        }
    }
}
