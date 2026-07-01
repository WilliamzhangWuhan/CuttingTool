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
        private const uint CfText = 1;
        private const uint CfBitmap = 2;
        private const uint CfMetafilePict = 3;
        private const uint CfOemText = 7;
        private const uint CfDib = 8;
        private const uint CfUnicodeText = 13;
        private const uint CfEnhMetafile = 14;
        private const uint CfHdrop = 15;
        private const uint CfDibV5 = 17;

        private static readonly string[] ImageFormatNames =
        {
            "PNG",
            "JFIF",
            "GIF"
        };

        private static readonly string[] NonImageFormatNames =
        {
            "HTML Format",
            "Rich Text Format",
            "Text",
            "Unicode Text",
            "CSV",
            "FileName",
            "FileNameW",
            "Object Descriptor",
            "Link Source",
            "Embed Source",
            "Native",
            "OwnerLink",
            "Ole Private Data",
            "DataObject",
            "Art::GVML ClipFormat",
            "Microsoft Office Drawing Shape Format",
            "PowerPoint 12.0 Internal Slides",
            "PowerPoint 12.0 Internal Shapes",
            "PowerPoint 14.0 Internal Slides",
            "PowerPoint 14.0 Internal Shapes",
            "PowerPoint 15.0 Internal Slides",
            "PowerPoint 15.0 Internal Shapes",
            "PowerPoint 16.0 Internal Slides",
            "PowerPoint 16.0 Internal Shapes"
        };

        public static ClipboardImageReadStatus TryReadImage(out BitmapSource image)
        {
            image = null;

            try
            {
                if (!ClipboardLooksLikeStandaloneImage())
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

        private static bool ClipboardLooksLikeStandaloneImage()
        {
            if (!HasImageFormat())
            {
                return false;
            }

            if (HasStandardNonImageFormat())
            {
                return false;
            }

            if (HasAnyRegisteredFormat(NonImageFormatNames))
            {
                return false;
            }

            return true;
        }

        private static bool HasImageFormat()
        {
            return NativeMethods.IsClipboardFormatAvailable(CfBitmap)
                || NativeMethods.IsClipboardFormatAvailable(CfDib)
                || NativeMethods.IsClipboardFormatAvailable(CfDibV5)
                || HasAnyRegisteredFormat(ImageFormatNames);
        }

        private static bool HasStandardNonImageFormat()
        {
            return NativeMethods.IsClipboardFormatAvailable(CfText)
                || NativeMethods.IsClipboardFormatAvailable(CfOemText)
                || NativeMethods.IsClipboardFormatAvailable(CfUnicodeText)
                || NativeMethods.IsClipboardFormatAvailable(CfMetafilePict)
                || NativeMethods.IsClipboardFormatAvailable(CfEnhMetafile)
                || NativeMethods.IsClipboardFormatAvailable(CfHdrop);
        }

        private static bool HasAnyRegisteredFormat(string[] formatNames)
        {
            foreach (string formatName in formatNames)
            {
                uint format = NativeMethods.RegisterClipboardFormat(formatName);
                if (format != 0 && NativeMethods.IsClipboardFormatAvailable(format))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
