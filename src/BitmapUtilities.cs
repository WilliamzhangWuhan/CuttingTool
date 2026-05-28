using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ScreenshotPin
{
    internal static class BitmapUtilities
    {
        public static BitmapSource CloneForUi(BitmapSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }

            BitmapSource working = source;
            if (working.Format != PixelFormats.Pbgra32 && working.Format != PixelFormats.Bgra32)
            {
                working = new FormatConvertedBitmap(working, PixelFormats.Pbgra32, null, 0);
            }

            var copy = new WriteableBitmap(working);
            if (copy.CanFreeze)
            {
                copy.Freeze();
            }

            return copy;
        }

        public static double SafeDpiX(BitmapSource source)
        {
            return source != null && source.DpiX > 1 ? source.DpiX : 96.0;
        }

        public static double SafeDpiY(BitmapSource source)
        {
            return source != null && source.DpiY > 1 ? source.DpiY : 96.0;
        }

        public static double PixelWidthToDips(BitmapSource source)
        {
            return source.PixelWidth * 96.0 / SafeDpiX(source);
        }

        public static double PixelHeightToDips(BitmapSource source)
        {
            return source.PixelHeight * 96.0 / SafeDpiY(source);
        }
    }
}
