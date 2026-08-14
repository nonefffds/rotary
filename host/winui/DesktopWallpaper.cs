using System;
using System.Runtime.InteropServices;

namespace RotaryMonitor
{
    public enum DesktopWallpaperPosition
    {
        Center = 0,
        Tile = 1,
        Stretch = 2,
        Fit = 3,
        Fill = 4,
        Span = 5,
    }

    public enum DesktopWallpaperSlideshowDirection { Forward = 0, Backward = 1 }
    public enum DesktopWallpaperSlideshowOptions { None = 0, ShuffleImages = 1 }
    public enum DesktopWallpaperStatus { Unknown = 0, Initialized = 1, Slideshow = 2 }

    [ComImport, Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IDesktopWallpaper
    {
        int SetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                         [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);
        int GetWallpaper([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                         [MarshalAs(UnmanagedType.LPWStr)] out string wallpaper);
        int GetMonitorDevicePathAt(uint monitorIndex,
                         [MarshalAs(UnmanagedType.LPWStr)] out string monitorID);
        int GetMonitorDevicePathCount(out uint count);
        int GetMonitorRECT([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                         out Win32.RECT displayRect);
        int SetBackgroundColor(uint color);
        int GetBackgroundColor(out uint color);
        int SetPosition(DesktopWallpaperPosition position);
        int GetPosition(out DesktopWallpaperPosition position);
        int SetSlideshow(IntPtr items);
        int GetSlideshow(IntPtr items, out DesktopWallpaperSlideshowDirection direction);
        int SetSlideshowOptions(DesktopWallpaperSlideshowDirection direction,
                         DesktopWallpaperSlideshowOptions options);
        int GetSlideshowOptions(out DesktopWallpaperSlideshowDirection direction,
                         out DesktopWallpaperSlideshowOptions options);
        int AdvanceSlideshow([MarshalAs(UnmanagedType.LPWStr)] string monitorID,
                         DesktopWallpaperSlideshowDirection direction);
        int GetStatus(out DesktopWallpaperStatus status);
    }

    /// <summary>Per-monitor wallpaper COM object (IDesktopWallpaper, Windows 10/11).
    /// Avoids composing/positioning a spanning image: each monitor gets its own
    /// wallpaper directly, and untouched monitors keep whatever they had.</summary>
    public static class DesktopWallpaper
    {
        private static readonly Guid CLSID_DesktopWallpaper =
            new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

        public static IDesktopWallpaper Create()
        {
            Type t = Type.GetTypeFromCLSID(CLSID_DesktopWallpaper);
            return (IDesktopWallpaper)Activator.CreateInstance(t);
        }

        public static string Pick(string primary, string fallback, string last)
        {
            if (!string.IsNullOrEmpty(primary) && System.IO.File.Exists(primary))
                return primary;
            if (!string.IsNullOrEmpty(fallback) && System.IO.File.Exists(fallback))
                return fallback;
            return last;
        }
    }
}
