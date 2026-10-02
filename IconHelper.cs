using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace ScreensaverExtender
{
    public static class IconHelper
    {
        /// <summary>
        /// Dynamically creates a clean, modern tray icon without requiring an external .ico file.
        /// Draws a modern circular shield/clock badge in high fidelity.
        /// </summary>
        public static Icon CreateAppIcon()
        {
            using (Bitmap bmp = new Bitmap(32, 32))
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                // Background Circle (Deep Blue / Indigo)
                using (Brush brush = new SolidBrush(Color.FromArgb(30, 136, 229))) // Material Blue 600
                {
                    g.FillEllipse(brush, 2, 2, 28, 28);
                }

                // Inner Accent Circle (Teal / Cyan)
                using (Pen pen = new Pen(Color.FromArgb(179, 229, 252), 2f)) // Light Blue
                {
                    g.DrawEllipse(pen, 5, 5, 22, 22);
                }

                // Center Clock/Sleep-Z / Shield glyph
                using (Pen clockHand = new Pen(Color.White, 2.2f))
                {
                    clockHand.StartCap = LineCap.Round;
                    clockHand.EndCap = LineCap.Round;

                    // Center to top-right
                    g.DrawLine(clockHand, 16, 16, 16, 9);
                    // Center to right
                    g.DrawLine(clockHand, 16, 16, 22, 16);
                }

                // Small center pivot
                using (Brush whiteBrush = new SolidBrush(Color.White))
                {
                    g.FillEllipse(whiteBrush, 14, 14, 4, 4);
                }

                IntPtr hIcon = bmp.GetHicon();
                return Icon.FromHandle(hIcon);
            }
        }
    }
}
