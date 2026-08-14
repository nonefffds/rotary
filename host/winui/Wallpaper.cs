using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace RotaryMonitor
{
    public static class Wallpaper
    {
        /// <summary>Build a desktop-spanning image.
        /// The rotation monitor gets its own landscape/portrait image (by its
        /// current orientation). The other monitors get:
        ///  - <paramref name="currentWallpaper"/> when <paramref name="changeRest"/> is
        ///    false (so they keep their existing look),
        ///  - the rotation monitor's image when <paramref name="restFollowRotation"/>
        ///    is true (may crop),
        ///  - otherwise their own rest landscape/portrait pair, chosen by the rotation
        ///    monitor's current orientation.
        /// Missing images fall back to the current wallpaper, then a neutral color.</summary>
        public static string Compose(string rotLandscape, string rotPortrait,
            string restLandscape, string restPortrait, string currentWallpaper,
            List<Win32.MonitorInfo> monitors, string rotationTarget,
            bool changeRest, bool restFollowRotation)
        {
            int minX = int.MaxValue, minY = int.MaxValue;
            int maxX = int.MinValue, maxY = int.MinValue;
            foreach (Win32.MonitorInfo m in monitors)
            {
                Win32.RECT p = Win32.PhysicalRect(m);   // physical pixels (DPI-aware)
                minX = Math.Min(minX, p.Left);
                minY = Math.Min(minY, p.Top);
                maxX = Math.Max(maxX, p.Right);
                maxY = Math.Max(maxY, p.Bottom);
            }

            var canvas = new Bitmap(maxX - minX, maxY - minY);
            using (var g = Graphics.FromImage(canvas))
            {
                g.Clear(Color.FromArgb(28, 28, 28));
            }

            bool rotIsPortrait = false;
            try
            {
                uint o = Win32.CurrentOrientation(rotationTarget);
                rotIsPortrait = o == 1 || o == 3;
            }
            catch { }

            string rotationImage = Pick(rotIsPortrait ? rotPortrait : rotLandscape,
                rotIsPortrait ? rotLandscape : rotPortrait, currentWallpaper);
            string restImage;
            if (!changeRest)
                restImage = currentWallpaper;
            else if (restFollowRotation)
                restImage = rotationImage;
            else
                restImage = Pick(rotIsPortrait ? restPortrait : restLandscape,
                    rotIsPortrait ? restLandscape : restPortrait, currentWallpaper);

            foreach (Win32.MonitorInfo m in monitors)
            {
                string path = m.DeviceName == rotationTarget ? rotationImage : restImage;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    continue;
                Win32.RECT p = Win32.PhysicalRect(m);   // physical pixels (DPI-aware)
                int w = p.Right - p.Left;
                int h = p.Bottom - p.Top;
                try
                {
                    using (Image src = Image.FromFile(path))
                    {
                        // cover-crop: upscale to cover the dest, then take a
                        // centered w x h crop. The SOURCE rect must be expressed
                        // in the ORIGINAL image pixels (srcW/srcH), not the dest.
                        float scale = Math.Max((float)w / src.Width, (float)h / src.Height);
                        int sw = (int)Math.Round(src.Width * scale);
                        int sh = (int)Math.Round(src.Height * scale);
                        int sx = (sw - w) / 2;
                        int sy = (sh - h) / 2;
                        int srcW = (int)Math.Round(w / scale);
                        int srcH = (int)Math.Round(h / scale);
                        int srcX = (int)Math.Round(sx / scale);
                        int srcY = (int)Math.Round(sy / scale);
                        using (var g = Graphics.FromImage(canvas))
                        {
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            g.DrawImage(src,
                                new Rectangle(p.Left - minX, p.Top - minY, w, h),
                                new Rectangle(srcX, srcY, srcW, srcH), GraphicsUnit.Pixel);
                        }
                    }
                }
                catch
                {
                    // one bad image must not abort the whole wallpaper
                }
            }

            // unique temp file so it never collides with the current wallpaper
            string tmp = Path.Combine(Path.GetTempPath(),
                "rotary_wallpaper_" + Guid.NewGuid().ToString("N").Substring(0, 8) + ".png");
            canvas.Save(tmp, System.Drawing.Imaging.ImageFormat.Png);
            canvas.Dispose();
            return tmp;
        }

        private static string Pick(string primary, string fallback, string last)
        {
            if (!string.IsNullOrEmpty(primary) && File.Exists(primary))
                return primary;
            if (!string.IsNullOrEmpty(fallback) && File.Exists(fallback))
                return fallback;
            return last;
        }
    }
}
