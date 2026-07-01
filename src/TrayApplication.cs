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
        private readonly ScreenshotIntentTracker _screenshotIntentTracker;
        private ClipboardWatcher _clipboardWatcher;
        private Forms.NotifyIcon _notifyIcon;
        private Forms.ToolStripMenuItem _autoPinItem;
        private Forms.ToolStripMenuItem _topmostItem;
        private bool _disposed;

        public TrayApplication(Application application)
        {
            _application = application ?? throw new ArgumentNullException("application");
            _captureManager = new CaptureManager();
            _screenshotIntentTracker = new ScreenshotIntentTracker();
        }

        public void Start()
        {
            CreateTrayIcon();

            _clipboardWatcher = new ClipboardWatcher(_application.Dispatcher, _screenshotIntentTracker);
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

            _screenshotIntentTracker.Dispose();
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
            _autoPinItem = new Forms.ToolStripMenuItem("\u81EA\u52A8\u951A\u5B9A\u622A\u56FE")
            {
                Checked = true,
                CheckOnClick = true
            };
            _autoPinItem.CheckedChanged += delegate
            {
                _captureManager.AutoPinEnabled = _autoPinItem.Checked;
            };

            _topmostItem = new Forms.ToolStripMenuItem("\u951A\u5B9A\u7A97\u53E3\u7F6E\u4E8E\u6700\u4E0A\u5C42")
            {
                Checked = true,
                CheckOnClick = true
            };
            _topmostItem.CheckedChanged += delegate
            {
                _captureManager.PinsTopmost = _topmostItem.Checked;
            };

            var pinCurrentItem = new Forms.ToolStripMenuItem("\u951A\u5B9A\u5F53\u524D\u526A\u8D34\u677F\u56FE\u7247");
            pinCurrentItem.Click += delegate { Dispatch(_captureManager.PinCurrentClipboardImage); };

            var restoreMinimizedItem = new Forms.ToolStripMenuItem("\u6062\u590D\u6700\u5C0F\u5316\u7A97\u53E3");
            restoreMinimizedItem.Click += delegate { Dispatch(_captureManager.RestoreMinimizedWindows); };

            var closeAllItem = new Forms.ToolStripMenuItem("\u5173\u95ED\u6240\u6709\u951A\u5B9A\u7A97\u53E3");
            closeAllItem.Click += delegate { Dispatch(_captureManager.CloseAll); };

            var exitItem = new Forms.ToolStripMenuItem("\u9000\u51FA");
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
