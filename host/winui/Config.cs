using System;
using System.Collections.Generic;
using System.IO;

namespace RotaryMonitor
{
    public class AppConfig
    {
        public string ComPort = "COM3";
        public bool ApplyOnStartup = true;
        public string WallpaperMode = "both";   // legacy; migrated to the toggles below
        public bool EnableWallpaper = true;
        public bool ChangeRestWallpaper = false;
        public bool RestFollowRotation = false;
        public string LandscapeWallpaper = "";
        public string PortraitWallpaper = "";
        public string RestLandscapeWallpaper = "";
        public string RestPortraitWallpaper = "";
        public string SecondaryColor = "#000000";
        public int State1To = 1;   // firmware state 1 -> dmdo (1 or 3)
        public int State3To = 3;   // firmware state 3 -> dmdo (1 or 3)
        public string Language = "";  // "", "en-US", "zh-CN", "ja-JP"
        public bool StartWithWindows = false;
        public bool AutoRestart = false;
        public bool AutoConnect = false;
        public string RotateMonitor = "";   // device name of the monitor to rotate ("" = primary)
        public double MountOffset = 0;      // degrees; sensor mounting offset from calibration

        public static string DefaultPath
        {
            get
            {
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Rotary");
                return Path.Combine(dir, "rotary.config");
            }
        }

        /// <summary>Old location (next to the exe); used to migrate existing profiles.</summary>
        public static string LegacyPath
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rotary.config"); }
        }

        public static AppConfig Load(string path)
        {
            var c = new AppConfig();
            if (!File.Exists(path) && File.Exists(LegacyPath))
                path = LegacyPath;   // migrate an old profile stored next to the exe
            if (!File.Exists(path))
                return c;
            var keys = new HashSet<string>();
            foreach (string line in File.ReadAllLines(path))
            {
                int i = line.IndexOf('=');
                if (i <= 0)
                    continue;
                string k = line.Substring(0, i).Trim();
                string v = line.Substring(i + 1).Trim();
                keys.Add(k);
                switch (k)
                {
                    case "comPort": c.ComPort = v; break;
                    case "applyOnStartup": c.ApplyOnStartup = v == "true"; break;
                    case "wallpaperMode": c.WallpaperMode = v; break;
                    case "enableWallpaper": c.EnableWallpaper = v == "true"; break;
                    case "changeRestWallpaper": c.ChangeRestWallpaper = v == "true"; break;
                    case "restFollowRotation": c.RestFollowRotation = v == "true"; break;
                    case "landscapeWallpaper": c.LandscapeWallpaper = v; break;
                    case "portraitWallpaper": c.PortraitWallpaper = v; break;
                    case "restLandscapeWallpaper": c.RestLandscapeWallpaper = v; break;
                    case "restPortraitWallpaper": c.RestPortraitWallpaper = v; break;
                    case "secondaryColor": c.SecondaryColor = v; break;
                    case "state1To":
                        int s1; if (int.TryParse(v, out s1)) c.State1To = s1;
                        break;
                    case "state3To":
                        int s3; if (int.TryParse(v, out s3)) c.State3To = s3;
                        break;
                    case "language": c.Language = v; break;
                    case "startWithWindows": c.StartWithWindows = v == "true"; break;
                    case "autoRestart": c.AutoRestart = v == "true"; break;
                    case "autoConnect": c.AutoConnect = v == "true"; break;
                    case "rotateMonitor": c.RotateMonitor = v; break;
                    case "mountOffset":
                        double mo; if (double.TryParse(v,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture, out mo))
                            c.MountOffset = mo;
                        break;
                }
            }
            // migrate the legacy wallpaperMode radio to the new toggles
            if (!keys.Contains("changeRestWallpaper") && keys.Contains("wallpaperMode"))
            {
                if (c.WallpaperMode == "both")
                {
                    c.ChangeRestWallpaper = true;
                    c.RestFollowRotation = true;
                }
                else
                {
                    c.ChangeRestWallpaper = false;
                    c.RestFollowRotation = false;
                }
            }
            return c;
        }

        public void Save(string path)
        {
            var lines = new List<string>
            {
                "comPort=" + ComPort,                "applyOnStartup=" + (ApplyOnStartup ? "true" : "false"),
                "enableWallpaper=" + (EnableWallpaper ? "true" : "false"),
                "changeRestWallpaper=" + (ChangeRestWallpaper ? "true" : "false"),
                "restFollowRotation=" + (RestFollowRotation ? "true" : "false"),
                "landscapeWallpaper=" + LandscapeWallpaper,
                "portraitWallpaper=" + PortraitWallpaper,
                "restLandscapeWallpaper=" + RestLandscapeWallpaper,
                "restPortraitWallpaper=" + RestPortraitWallpaper,
                "secondaryColor=" + SecondaryColor,
                "state1To=" + State1To,
                "state3To=" + State3To,
                "language=" + Language,
                "startWithWindows=" + (StartWithWindows ? "true" : "false"),
                "autoRestart=" + (AutoRestart ? "true" : "false"),
                "autoConnect=" + (AutoConnect ? "true" : "false"),
                "rotateMonitor=" + RotateMonitor,
                "mountOffset=" + MountOffset.ToString("0.##",
                    System.Globalization.CultureInfo.InvariantCulture),
            };
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllLines(path, lines);
        }
    }
}
