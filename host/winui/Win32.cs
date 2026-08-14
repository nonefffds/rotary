using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RotaryMonitor
{
    public static class Win32
    {
        public const int ENUM_CURRENT_SETTINGS = -1;
        public const uint CDS_TEST = 0x00000002;
        public const uint CDS_UPDATEREGISTRY = 0x00000001;
        public const uint DM_DISPLAYORIENTATION = 0x00000080;
        public const uint DM_PELSWIDTH = 0x00080000;
        public const uint DM_PELSHEIGHT = 0x00100000;
        public const int DISP_CHANGE_SUCCESSFUL = 0;
        public const uint DISPLAY_DEVICE_PRIMARY = 0x00000004;
        public const uint MONITORINFOF_PRIMARY = 0x00000001;
        public const uint SPI_SETDESKWALLPAPER = 0x0014;
        public const uint SPIF_UPDATEINIFILE = 0x0001;
        public const uint SPIF_SENDWININICHANGE = 0x0002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINTL
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DEVMODE
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
            public ushort dmSpecVersion;
            public ushort dmDriverVersion;
            public ushort dmSize;
            public ushort dmDriverExtra;
            public uint dmFields;
            public POINTL dmPosition;
            public uint dmDisplayOrientation;
            public uint dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
            public ushort dmLogPixels;
            public uint dmBitsPerPel;
            public uint dmPelsWidth;
            public uint dmPelsHeight;
            public uint dmDisplayFlags;
            public uint dmDisplayFrequency;
            public uint dmICMMethod;
            public uint dmICMIntent;
            public uint dmMediaType;
            public uint dmDitherType;
            public uint dmReserved1;
            public uint dmReserved2;
            public uint dmPanningWidth;
            public uint dmPanningHeight;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct DISPLAY_DEVICE
        {
            public uint cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceString;
            public uint StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string DeviceKey;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct MONITORINFOEXW
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        public class MonitorInfo
        {
            public RECT Rect;
            public bool IsPrimary;
            public string DeviceName = "";
        }

        public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdc, ref RECT lprcMonitor, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum,
            ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum,
            ref DEVMODE lpDevMode);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ChangeDisplaySettingsEx(string lpszDeviceName, ref DEVMODE lpDevMode,
            IntPtr hwnd, uint dwflags, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            MonitorEnumProc lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEXW lpmi);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern bool SystemParametersInfo(uint uiAction, uint uiParam,
            string pvParam, uint fWinIni);

        // Windows Error Reporting: relaunches the app automatically if it crashes.
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int RegisterApplicationRestart(string pwzCommandLine, int dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int UnregisterApplicationRestart();

        public static string PrimaryDeviceName()
        {
            var dd = new DISPLAY_DEVICE();
            dd.cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            for (uint i = 0; EnumDisplayDevices(null, i, ref dd, 0); i++)
            {
                if ((dd.StateFlags & DISPLAY_DEVICE_PRIMARY) != 0)
                    return dd.DeviceName;
                dd = new DISPLAY_DEVICE();
                dd.cb = (uint)Marshal.SizeOf(typeof(DISPLAY_DEVICE));
            }
            throw new InvalidOperationException("No primary display device found.");
        }

        public static DEVMODE GetDevMode(string device)
        {
            var dm = new DEVMODE();
            dm.dmSize = (ushort)Marshal.SizeOf(typeof(DEVMODE));
            if (!EnumDisplaySettings(device, ENUM_CURRENT_SETTINGS, ref dm))
                throw new InvalidOperationException("EnumDisplaySettings failed for " + device);
            return dm;
        }

        public static uint CurrentOrientation(string device)
        {
            return GetDevMode(device).dmDisplayOrientation;
        }

        public static int SetOrientation(string device, uint targetDmdo)
        {
            var dm = GetDevMode(device);
            uint cur = dm.dmDisplayOrientation;
            if (cur == targetDmdo)
                return DISP_CHANGE_SUCCESSFUL;

            if ((targetDmdo == 1 || targetDmdo == 3) && cur == 0)
            {
                uint t = dm.dmPelsWidth;
                dm.dmPelsWidth = dm.dmPelsHeight;
                dm.dmPelsHeight = t;
            }
            else if (targetDmdo == 0 && (cur == 1 || cur == 3))
            {
                uint t = dm.dmPelsWidth;
                dm.dmPelsWidth = dm.dmPelsHeight;
                dm.dmPelsHeight = t;
            }
            dm.dmDisplayOrientation = targetDmdo;
            dm.dmFields |= DM_DISPLAYORIENTATION | DM_PELSWIDTH | DM_PELSHEIGHT;

            int test = ChangeDisplaySettingsEx(device, ref dm, IntPtr.Zero, CDS_TEST, IntPtr.Zero);
            if (test != DISP_CHANGE_SUCCESSFUL)
                return test;
            return ChangeDisplaySettingsEx(device, ref dm, IntPtr.Zero, CDS_UPDATEREGISTRY, IntPtr.Zero);
        }

        public static List<MonitorInfo> EnumMonitors()
        {
            var list = new List<MonitorInfo>();
            MonitorEnumProc cb = delegate(IntPtr h, IntPtr hdc, ref RECT rect, IntPtr l)
            {
                var mi = new MONITORINFOEXW();
                mi.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFOEXW));
                if (GetMonitorInfo(h, ref mi))
                {
                    list.Add(new MonitorInfo
                    {
                        Rect = mi.rcMonitor,
                        IsPrimary = (mi.dwFlags & MONITORINFOF_PRIMARY) != 0,
                        DeviceName = mi.szDevice,
                    });
                }
                return true;
            };
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, cb, IntPtr.Zero);
            return list;
        }

        private static void SetWallpaperStyle(string style)
        {
            string wallpaperStyle = "10";
            string tile = "0";
            if (style == "fit") wallpaperStyle = "6";
            else if (style == "center") wallpaperStyle = "0";
            else if (style == "tile") { wallpaperStyle = "0"; tile = "1"; }

            using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
            {
                k.SetValue("WallpaperStyle", wallpaperStyle);
                k.SetValue("TileWallpaper", tile);
            }
        }

        public static void SetWallpaper(string path, string style)
        {
            SetWallpaperStyle(style);
            SystemParametersInfo(SPI_SETDESKWALLPAPER, 0, path,
                SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE);
        }
    }
}
