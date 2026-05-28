using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;

namespace ScreenshotPin
{
    internal enum ClipboardImageReadStatus
    {
        Success,
        NoImage,
        Busy,
        Error
    }

    internal static class ClipboardImageReader
    {
        public static ClipboardImageReadStatus TryReadImage(out BitmapSource image)
        {
            image = null;

            try
            {
                if (!Clipboard.ContainsImage())
                {
                    return ClipboardImageReadStatus.NoImage;
                }

                BitmapSource clipboardImage = Clipboard.GetImage();
                if (clipboardImage == null)
                {
                    return ClipboardImageReadStatus.NoImage;
                }

                image = BitmapUtilities.CloneForUi(clipboardImage);
                return ClipboardImageReadStatus.Success;
            }
            catch (COMException)
            {
                return ClipboardImageReadStatus.Busy;
            }
            catch (ExternalException)
            {
                return ClipboardImageReadStatus.Busy;
            }
            catch
            {
                return ClipboardImageReadStatus.Error;
            }
        }
    }
}
