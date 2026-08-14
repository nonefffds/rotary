using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace RotaryMonitor
{
    public static class Wallpaper
    {
        /// <summary>Build a desktop-spanning image. When <paramref name="showOnAll"/>
        /// is false, only the monitor whose DeviceName equals <paramref name="targetDevice"/>
        /// shows a wallpaper; every other monitor is filled with <paramref name="secondary"/>.
        /// Each wallpaper-bearing monitor uses the landscape or portrait image according to
        /// its own current orientation (so a landscape sub-monitor is never cropped by a
        /// portrait image).</summary>
        public static string Compose(string landscapeImage, string portraitImage,
            List<Win32.MonitorInfo> monitors, Color secondary, bool showOnAll, string targetDevice)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (Win32.MonitorInfo m in monitors)
            {
                minX = Math.Min(minX, m.Rect.Left);
                minY = Math.Min(minY, m.Rect.Top);
                maxX = Math.Max(maxX, m.Rect.Right);
                maxY = Math.Max(maxY, m.Rect.Bottom);
            }

            var canvas = new Bitmap(maxX - minX, maxY - minY);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(secondary);
            }

            foreach (Win32.MonitorInfo m in monitors)
            {
                if (!showOnAll && m.DeviceName != targetDevice)
                    continue;
                int w = m.Rect.Right - m.Rect.Left;
                int h = m.Rect.Bottom - m.Rect.Top;
                string path = ImageFor(m, landscapeImage, portraitImage);
                if (string.IsNullOrEmpty(path))
                    continue;
                using (Image src = Image.FromFile(path))
                {
                    float scale = Math.Max((float)w / src.Width, (float)h / src.Height);
                    int sw = (int)Math.Round(src.Width * scale);
                    int sh = (int)Math.Round(src.Height * scale);
                    int sx = (sw - w) / 2;
                    int sy = (sh - h) / 2;
                    using (var g = Graphics.FromImage(canvas))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(src,
                            new Rectangle(m.Rect.Left - minX, m.Rect.Top - minY, w, h),
                            new Rectangle(sx, sy, w, h), GraphicsUnit.Pixel);
                    }
                }
            }

            string tmp = Path.Combine(Path.GetTempPath(), "rotary_wallpaper.png");
            canvas.Save(tmp, System.Drawing.Imaging.ImageFormat.Png);
            canvas.Dispose();
            return tmp;
        }

        private static string ImageFor(Win32.MonitorInfo m, string landscape, string portrait)
        {
            bool isPortrait = false;
            try
            {
                uint o = Win32.CurrentOrientation(m.DeviceName);
                isPortrait = o == 1 || o == 3;
            }
            catch { }

            string primary = isPortrait ? portrait : landscape;
            string fallback = isPortrait ? landscape : portrait;
            if (!string.IsNullOrEmpty(primary) && File.Exists(primary))
                return primary;
            if (!string.IsNullOrEmpty(fallback) && File.Exists(fallback))
                return fallback;
            return "";
        }
    }
}
