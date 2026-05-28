using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Media.Imaging;

namespace ScreenshotPin
{
    internal static class ImageHasher
    {
        public static string Compute(BitmapSource source)
        {
            if (source == null)
            {
                return string.Empty;
            }

            using (var stream = new MemoryStream())
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(source));
                encoder.Save(stream);

                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(stream.ToArray());
                    return BitConverter.ToString(hash).Replace("-", string.Empty);
                }
            }
        }
    }
}
