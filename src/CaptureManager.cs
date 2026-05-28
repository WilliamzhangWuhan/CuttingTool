using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenshotPin
{
    internal sealed class CaptureManager : IDisposable
    {
        private const int MaxRememberedHashes = 40;

        private readonly List<PinWindow> _windows = new List<PinWindow>();
        private readonly Queue<string> _recentHashOrder = new Queue<string>();
        private readonly HashSet<string> _recentHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly Queue<string> _ignoredHashOrder = new Queue<string>();
        private readonly HashSet<string> _ignoredHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private bool _pinsTopmost = true;

        public bool AutoPinEnabled { get; set; }

        public bool PinsTopmost
        {
            get { return _pinsTopmost; }
            set
            {
                if (_pinsTopmost == value)
                {
                    return;
                }

                _pinsTopmost = value;
                PinWindow[] windows = _windows.ToArray();
                foreach (PinWindow window in windows)
                {
                    window.SetPinnedTopmost(value);
                }
            }
        }

        public CaptureManager()
        {
            AutoPinEnabled = true;
        }

        public void HandleClipboardImage(BitmapSource image)
        {
            if (!AutoPinEnabled)
            {
                return;
            }

            PinImage(image, false);
        }

        public void PinCurrentClipboardImage()
        {
            BitmapSource image;
            ClipboardImageReadStatus status = ClipboardImageReader.TryReadImage(out image);
            if (status == ClipboardImageReadStatus.Success)
            {
                PinImage(image, true);
                return;
            }

            MessageBox.Show(
                "当前剪贴板里没有可锚定的图片。",
                "Screenshot Pin",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void IgnoreClipboardHash(string hash)
        {
            if (string.IsNullOrWhiteSpace(hash))
            {
                return;
            }

            if (_ignoredHashes.Add(hash))
            {
                _ignoredHashOrder.Enqueue(hash);
                TrimHashQueue(_ignoredHashes, _ignoredHashOrder);
            }
        }

        public void CloseAll()
        {
            PinWindow[] windows = _windows.ToArray();
            foreach (PinWindow window in windows)
            {
                window.Close();
            }
        }

        public void RestoreMinimizedWindows()
        {
            PinWindow[] windows = _windows.ToArray();
            foreach (PinWindow window in windows)
            {
                window.RestoreFromMinimized();
            }
        }

        public void Dispose()
        {
            CloseAll();
        }

        private void PinImage(BitmapSource image, bool force)
        {
            if (image == null)
            {
                return;
            }

            string hash = ImageHasher.Compute(image);

            if (!force && _ignoredHashes.Remove(hash))
            {
                return;
            }

            if (!force && _recentHashes.Contains(hash))
            {
                return;
            }

            RememberHash(hash);

            var window = new PinWindow(image, PinsTopmost, IgnoreClipboardHash);
            window.Closed += delegate { _windows.Remove(window); };
            _windows.Add(window);
            window.Show();
            window.Activate();
        }

        private void RememberHash(string hash)
        {
            if (_recentHashes.Add(hash))
            {
                _recentHashOrder.Enqueue(hash);
                TrimHashQueue(_recentHashes, _recentHashOrder);
            }
        }

        private static void TrimHashQueue(HashSet<string> hashSet, Queue<string> order)
        {
            while (order.Count > MaxRememberedHashes)
            {
                string oldHash = order.Dequeue();
                hashSet.Remove(oldHash);
            }
        }
    }
}
