using System;
using System.Windows.Threading;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace ScreenshotPin
{
    internal sealed class ClipboardWatcher : IDisposable
    {
        private readonly Dispatcher _dispatcher;
        private readonly DispatcherTimer _readTimer;
        private HwndSource _source;
        private int _busyRetryCount;
        private bool _disposed;

        public event EventHandler<BitmapSource> ImageAvailable;

        public ClipboardWatcher(Dispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException("dispatcher");
            _readTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher);
            _readTimer.Interval = TimeSpan.FromMilliseconds(160);
            _readTimer.Tick += OnReadTimerTick;
        }

        public void Start()
        {
            if (_source != null)
            {
                return;
            }

            var parameters = new HwndSourceParameters("ScreenshotPinClipboardWatcher");
            parameters.Width = 0;
            parameters.Height = 0;

            _source = new HwndSource(parameters);
            _source.AddHook(WndProc);
            NativeMethods.AddClipboardFormatListener(_source.Handle);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _readTimer.Stop();

            if (_source != null)
            {
                NativeMethods.RemoveClipboardFormatListener(_source.Handle);
                _source.RemoveHook(WndProc);
                _source.Dispose();
                _source = null;
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WmClipboardUpdate)
            {
                ScheduleClipboardRead();
                handled = true;
            }

            return IntPtr.Zero;
        }

        private void ScheduleClipboardRead()
        {
            if (_disposed)
            {
                return;
            }

            _busyRetryCount = 0;
            _readTimer.Stop();
            _readTimer.Start();
        }

        private void OnReadTimerTick(object sender, EventArgs e)
        {
            _readTimer.Stop();

            BitmapSource image;
            ClipboardImageReadStatus status = ClipboardImageReader.TryReadImage(out image);
            if (status == ClipboardImageReadStatus.Success)
            {
                EventHandler<BitmapSource> handler = ImageAvailable;
                if (handler != null)
                {
                    handler(this, image);
                }

                return;
            }

            if (status == ClipboardImageReadStatus.Busy && _busyRetryCount < 4)
            {
                _busyRetryCount++;
                _readTimer.Start();
            }
        }
    }
}
