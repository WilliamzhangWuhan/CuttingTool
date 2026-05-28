using System.Windows;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace ScreenshotPin
{
    internal static class ScreenUtilities
    {
        public static Rect GetCursorWorkAreaDips()
        {
            Forms.Screen screen = Forms.Screen.FromPoint(Forms.Control.MousePosition);
            Drawing.Rectangle bounds = screen.WorkingArea;

            using (Drawing.Graphics graphics = Drawing.Graphics.FromHwnd(System.IntPtr.Zero))
            {
                double scaleX = 96.0 / graphics.DpiX;
                double scaleY = 96.0 / graphics.DpiY;

                return new Rect(
                    bounds.Left * scaleX,
                    bounds.Top * scaleY,
                    bounds.Width * scaleX,
                    bounds.Height * scaleY);
            }
        }
    }
}
