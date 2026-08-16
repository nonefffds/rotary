# -*- coding: utf-8 -*-
"""Regenerate Localization.cs + Strings/*/Resources.resw from the tables in
Localization.cs, applying overrides and adding new keys.
Run from anywhere:  python host/winui/tools/gen_localization.py
"""
import os
import re
import xml.sax.saxutils as sax

HERE = os.path.dirname(os.path.abspath(__file__))
WINUI = os.path.dirname(HERE)
CS = os.path.join(WINUI, "Localization.cs")
STRINGS = os.path.join(WINUI, "Strings")


def unescape(s):
    return s.replace('\\"', '"').replace('\\\\', '\\')


def escape_cs(s):
    return s.replace('\\', '\\\\').replace('"', '\\"')


def parse_entries(section_text):
    d = {}
    for m in re.finditer(r'\{\s*"((?:[^"\\]|\\.)*)"\s*,\s*"((?:[^"\\]|\\.)*)"\s*\}', section_text):
        d[unescape(m.group(1))] = unescape(m.group(2))
    return d


def load():
    text = open(CS, encoding="utf-8").read()
    langs = {}
    pattern = re.compile(r'\{\s*"(en-US|zh-CN|ja-JP)"\s*,\s*new Dictionary<string, string>\s*\{')
    m = pattern.search(text)
    order = []
    while m:
        lang = m.group(1)
        start = m.end()
        nxt = pattern.search(text, start)
        if nxt:
            end = nxt.start()
        else:
            end = text.index('            };', start)
        close = text.rindex('        } },', start, end)
        langs[lang] = parse_entries(text[start:close])
        order.append(lang)
        m = nxt
    return langs, order


def emit_table(lang, data, indent="                "):
    lines = []
    for key in data:
        lines.append(indent + '{ "%s", "%s" },' % (key, escape_cs(data[key])))
    return "\n".join(lines)


def emit_cs(langs, order):
    header = """using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RotaryMonitor
{
    /// <summary>Self-contained localization. Strings are compiled into the
    /// assembly (no MRT/resources.pri dependency) for reliable behavior in
    /// unpackaged self-contained builds.</summary>
    public static class L
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Tables =
            new Dictionary<string, Dictionary<string, string>>
            {
"""
    body = header
    for lang in order:
        body += '                { "%s", new Dictionary<string, string>\n                {\n' % lang
        body += emit_table(lang, langs[lang]) + "\n"
        body += "                } },\n"
    body += """            };

        private static string _lang = "";

        public static void Init(string langOverride)
        {
            _lang = langOverride ?? "";
        }

        public static string Get(string key)
        {
            string lang = string.IsNullOrEmpty(_lang) ? SystemLanguage() : _lang;
            Dictionary<string, string> table;
            if (Tables.TryGetValue(lang, out table))
            {
                string v;
                if (table.TryGetValue(key, out v))
                    return v;
            }
            if (lang != "en-US" && Tables["en-US"].TryGetValue(key, out var en))
                return en;
            return key;
        }

        private static string SystemLanguage()
        {
            // Robust for unpackaged apps: read the OS UI language via P/Invoke.
            try
            {
                ushort lang = GetUserDefaultUILanguage();
                int primary = lang & 0x3FF;
                if (primary == 0x04) return "zh-CN";   // Chinese
                if (primary == 0x11) return "ja-JP";   // Japanese
                if (primary == 0x09) return "en-US";   // English
            }
            catch { }

            try
            {
                foreach (var l in Windows.Globalization.ApplicationLanguages.Languages)
                {
                    if (l.StartsWith("zh", StringComparison.OrdinalIgnoreCase)) return "zh-CN";
                    if (l.StartsWith("ja", StringComparison.OrdinalIgnoreCase)) return "ja-JP";
                    if (l.StartsWith("en", StringComparison.OrdinalIgnoreCase)) return "en-US";
                }
            }
            catch { }
            return "en-US";
        }

        [DllImport("kernel32.dll")]
        private static extern ushort GetUserDefaultUILanguage();
    }
}
"""
    open(CS, "w", encoding="utf-8").write(body)


def emit_resw(lang, data):
    lines = ['<?xml version="1.0" encoding="utf-8"?>', "<root>"]
    for key in data:
        lines.append('    <data name="%s" xml:space="preserve">' % sax.escape(key))
        lines.append("        <value>%s</value>" % sax.escape(data[key]))
        lines.append("    </data>")
    lines.append("</root>")
    path = os.path.join(STRINGS, lang, "Resources.resw")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w", encoding="utf-8").write("\n".join(lines) + "\n")


# ---------------- changes ----------------
NEW = {
    "en-US": {
        "AppTitle.Title": "Rotary",
        "PortConnect": "Connect",
        "PortDisconnect": "Disconnect",
        "MsgConnectFirst": "Connect to a COM port first.",
        "AutoConnect": "Auto-connect on startup",
        "NavAbout": "About",
        "AboutVersion": "Version: {0}",
        "AboutRepo": "GitHub repository",
        "CheckUpdates": "Check for updates",
        "UpToDate": "You have the latest version.",
        "UpdateAvailable": "Version {0} is available.",
        "UpdateFailed": "Could not check for updates.",
        "AboutNotConfigured": "Set your repository URL in the source to enable update checks.",
        "ExitButton": "Exit",
        "TrayShow": "Show",
        "TrayExit": "Exit",
        "TrayHint": "Rotary is still running in the tray.",
        "CalStep1": "Step 1 of 3 \u2014 Keep the monitor horizontal (landscape), then press Save baseline.",
        "CalBaseline": "Save baseline",
        "CalBaselineDone": "Baseline captured: {0}\u00B0",
        "CalStep2": "Step 2 of 3 \u2014 Choose the rotation direction, then rotate the monitor to that position.",
        "CalDirCW": "90\u00B0 \u2014 clockwise (right edge down)",
        "CalDirCCW": "270\u00B0 \u2014 counterclockwise (left edge down)",
        "CalStep3": "Step 3 of 3 \u2014 Press Save rotation.",
        "CalSaveRot": "Save rotation",
        "CalVerify": "Detected rotation: {0}\u00B0 (expected {1}\u00B0)",
        "CalMismatch": "Mismatch \u2014 detected {0}\u00B0, expected {1}\u00B0. Check the direction and try again.",
        "EnableWallpaper": "Change wallpaper when rotated",
        "RotMonHeader": "Rotation monitor",
        "ChangeRestWallpaper": "Change wallpaper on the other monitors",
        "FollowRotWallpaper": "Follow the rotation monitor's wallpaper (may crop)",
        "RestLandscapeLabel": "Other monitors \u2014 when the rotation monitor is landscape:",
        "RestPortraitLabel": "Other monitors \u2014 when the rotation monitor is portrait:",
        "MsgWallpaperDisabled": "Wallpaper change is disabled.",
        "SaveWallpaper": "Save wallpaper",
        "MsgSaved": "Saved",
        "CalNotConnected": "Not connected \u2014 connect to a COM port first.",
        "AboutFirmware": "Firmware repository",
        "ViewLicenses": "Third-party licenses",
        "SilentStartWithWindows": "Start silently in the background (no window)",
        "AutoStartMonitor": "Auto-start monitoring when connected",
        "CheckUpdatesOnStartup": "Check for updates on startup",
        "AboutTagline": "One turn, aligned.",
        "DownloadUpdate": "Download update",
        "DownloadingUpdate": "Downloading\u2026",
        "MsgUpdateReadyTitle": "Update ready",
        "MsgUpdateReadyBody": "Update downloaded to {0}. Exit Rotary and run the installer now?",
        "MsgInstallNow": "Install now",
        "MsgUpdateDownloadFailed": "Update download failed: {0}",
        "MsgAutoStartMonitor": "Monitoring auto-started.",
    },
    "zh-CN": {
        "AppTitle.Title": "Rotary",
        "NavMonitor": "\u663e\u793a\u5668\u8bbe\u7f6e",
        "PortConnect": "\u8fde\u63a5",
        "PortDisconnect": "\u65ad\u5f00",
        "MsgConnectFirst": "\u8bf7\u5148\u8fde\u63a5\u4e32\u53e3\u3002",
        "AutoConnect": "\u542f\u52a8\u65f6\u81ea\u52a8\u8fde\u63a5",
        "NavAbout": "\u5173\u4e8e",
        "AboutVersion": "\u7248\u672c\uff1a{0}",
        "AboutRepo": "GitHub \u4ed3\u5e93",
        "CheckUpdates": "\u68c0\u67e5\u66f4\u65b0",
        "UpToDate": "\u5df2\u662f\u6700\u65b0\u7248\u672c\u3002",
        "UpdateAvailable": "\u53d1\u73b0\u65b0\u7248\u672c {0}\u3002",
        "UpdateFailed": "\u65e0\u6cd5\u68c0\u67e5\u66f4\u65b0\u3002",
        "AboutNotConfigured": "\u8bf7\u5728\u6e90\u7801\u4e2d\u8bbe\u7f6e\u4ed3\u5e93\u5730\u5740\u4ee5\u542f\u7528\u66f4\u65b0\u68c0\u67e5\u3002",
        "ExitButton": "\u9000\u51fa",
        "TrayShow": "\u663e\u793a",
        "TrayExit": "\u9000\u51fa",
        "TrayHint": "Rotary \u4ecd\u5728\u6258\u76d8\u8fd0\u884c\u4e2d\u3002",
        "CalStep1": "\u7b2c 1 \u6b65\uff08\u5171 3 \u6b65\uff09\u2014\u2014\u4fdd\u6301\u663e\u793a\u5668\u6c34\u5e73\uff08\u6a2a\u5c4f\uff09\uff0c\u7136\u540e\u70b9\u51fb\u201c\u4fdd\u5b58\u57fa\u51c6\u201d\u3002",
        "CalBaseline": "\u4fdd\u5b58\u57fa\u51c6",
        "CalBaselineDone": "\u5df2\u6355\u83b7\u57fa\u51c6\uff1a{0}\u00b0",
        "CalStep2": "\u7b2c 2 \u6b65\uff08\u5171 3 \u6b65\uff09\u2014\u2014\u9009\u62e9\u65cb\u8f6c\u65b9\u5411\uff0c\u7136\u540e\u5c06\u663e\u793a\u5668\u65cb\u8f6c\u5230\u8be5\u4f4d\u7f6e\u3002",
        "CalDirCW": "90\u00b0 \u2014\u2014 \u987a\u65f6\u9488\uff08\u53f3\u8fb9\u7f18\u671d\u4e0b\uff09",
        "CalDirCCW": "270\u00b0 \u2014\u2014 \u9006\u65f6\u9488\uff08\u5de6\u8fb9\u7f18\u671d\u4e0b\uff09",
        "CalStep3": "\u7b2c 3 \u6b65\uff08\u5171 3 \u6b65\uff09\u2014\u2014\u70b9\u51fb\u201c\u4fdd\u5b58\u65cb\u8f6c\u201d\u3002",
        "CalSaveRot": "\u4fdd\u5b58\u65cb\u8f6c",
        "CalVerify": "\u68c0\u6d4b\u5230\u65cb\u8f6c\uff1a{0}\u00b0\uff08\u9884\u671f {1}\u00b0\uff09",
        "CalMismatch": "\u4e0d\u5339\u914d\u2014\u2014\u68c0\u6d4b\u5230 {0}\u00b0\uff0c\u9884\u671f {1}\u00b0\u3002\u8bf7\u68c0\u67e5\u65b9\u5411\u540e\u91cd\u8bd5\u3002",
        "EnableWallpaper": "\u65cb\u8f6c\u65f6\u66f4\u6362\u58c1\u7eb8",
        "RotMonHeader": "\u65cb\u8f6c\u663e\u793a\u5668",
        "ChangeRestWallpaper": "\u66f4\u6362\u5176\u4ed6\u663e\u793a\u5668\u7684\u58c1\u7eb8",
        "FollowRotWallpaper": "\u8ddf\u968f\u65cb\u8f6c\u663e\u793a\u5668\u7684\u58c1\u7eb8\uff08\u53ef\u80fd\u4f1a\u88ab\u88c1\u526a\uff09",
        "RestLandscapeLabel": "\u5176\u4ed6\u663e\u793a\u5668 \u2014\u2014 \u5f53\u65cb\u8f6c\u663e\u793a\u5668\u4e3a\u6a2a\u5c4f\u65f6\uff1a",
        "RestPortraitLabel": "\u5176\u4ed6\u663e\u793a\u5668 \u2014\u2014 \u5f53\u65cb\u8f6c\u663e\u793a\u5668\u4e3a\u7ad6\u5c4f\u65f6\uff1a",
        "MsgWallpaperDisabled": "\u58c1\u7eb8\u66f4\u6362\u5df2\u7981\u7528\u3002",
        "SaveWallpaper": "\u4fdd\u5b58\u58c1\u7eb8",
        "MsgSaved": "\u5df2\u4fdd\u5b58",
        "CalNotConnected": "\u672a\u8fde\u63a5\u2014\u2014\u8bf7\u5148\u8fde\u63a5\u4e32\u53e3\u3002",
        "AboutFirmware": "\u56fa\u4ef6\u4ed3\u5e93",
        "ViewLicenses": "\u7b2c\u4e09\u65b9\u8bb8\u53ef\u8bc1",
        "SilentStartWithWindows": "\u9759\u9ed8\u542f\u52a8\uff08\u4e0d\u663e\u793a\u7a97\u53e3\uff09",
        "AutoStartMonitor": "\u8fde\u63a5\u540e\u81ea\u52a8\u5f00\u59cb\u76d1\u63a7",
        "CheckUpdatesOnStartup": "\u542f\u52a8\u65f6\u68c0\u67e5\u66f4\u65b0",
        "AboutTagline": "\u4e00\u8f6c\uff0c\u5bf9\u9f50\u3002",
        "DownloadUpdate": "\u4e0b\u8f7d\u66f4\u65b0",
        "DownloadingUpdate": "\u6b63\u5728\u4e0b\u8f7d\u2026",
        "MsgUpdateReadyTitle": "\u66f4\u65b0\u5df2\u51c6\u5907\u5c31\u7eea",
        "MsgUpdateReadyBody": "\u66f4\u65b0\u5df2\u4e0b\u8f7d\u5230 {0}\u3002\u9000\u51fa Rotary \u5e76\u7acb\u5373\u8fd0\u884c\u5b89\u88c5\u7a0b\u5e8f\uff1f",
        "MsgInstallNow": "\u7acb\u5373\u5b89\u88c5",
        "MsgUpdateDownloadFailed": "\u66f4\u65b0\u4e0b\u8f7d\u5931\u8d25\uff1a{0}",
        "MsgAutoStartMonitor": "\u5df2\u81ea\u52a8\u5f00\u59cb\u76d1\u63a7\u3002",
    },
    "ja-JP": {
        "AppTitle.Title": "Rotary",
        "PortConnect": "\u63a5\u7d9a",
        "PortDisconnect": "\u5207\u65ad",
        "MsgConnectFirst": "\u5148\u306bCOM\u30dd\u30fc\u30c8\u3078\u63a5\u7d9a\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "AutoConnect": "\u8d77\u52d5\u6642\u306b\u81ea\u52d5\u63a5\u7d9a",
        "NavAbout": "\u30d0\u30fc\u30b8\u30e7\u30f3\u60c5\u5831",
        "AboutVersion": "\u30d0\u30fc\u30b8\u30e7\u30f3\uff1a{0}",
        "AboutRepo": "GitHub \u30ea\u30dd\u30b8\u30c8\u30ea",
        "CheckUpdates": "\u66f4\u65b0\u3092\u78ba\u8a8d",
        "UpToDate": "\u6700\u65b0\u30d0\u30fc\u30b8\u30e7\u30f3\u3067\u3059\u3002",
        "UpdateAvailable": "\u30d0\u30fc\u30b8\u30e7\u30f3 {0} \u304c\u5229\u7528\u53ef\u80fd\u3067\u3059\u3002",
        "UpdateFailed": "\u66f4\u65b0\u3092\u78ba\u8a8d\u3067\u304d\u307e\u305b\u3093\u3067\u3057\u305f\u3002",
        "AboutNotConfigured": "\u66f4\u65b0\u78ba\u8a8d\u3092\u6709\u52b9\u306b\u3059\u308b\u306b\u306f\u30bd\u30fc\u30b9\u3067\u30ea\u30dd\u30b8\u30c8\u30eaURL\u3092\u8a2d\u5b9a\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "ExitButton": "\u7d42\u4e86",
        "TrayShow": "\u8868\u793a",
        "TrayExit": "\u7d42\u4e86",
        "TrayHint": "Rotary \u306f\u30c8\u30ec\u30a4\u3067\u5b9f\u884c\u4e2d\u3067\u3059\u3002",
        "CalStep1": "\u624b\u9806 1/3 \u2014 \u30e2\u30cb\u30bf\u30fc\u3092\u6a2a\u5411\u304d\u306b\u4fdd\u3061\u3001\u300c\u57fa\u6e96\u3092\u4fdd\u5b58\u300d\u3092\u62bc\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "CalBaseline": "\u57fa\u6e96\u3092\u4fdd\u5b58",
        "CalBaselineDone": "\u57fa\u6e96\u3092\u53d6\u5f97\u3057\u307e\u3057\u305f\uff1a{0}\u00b0",
        "CalStep2": "\u624b\u9806 2/3 \u2014 \u56de\u8ee2\u65b9\u5411\u3092\u9078\u3073\u3001\u30e2\u30cb\u30bf\u30fc\u3092\u305d\u306e\u4f4d\u7f6e\u307e\u3067\u56de\u8ee2\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "CalDirCW": "90\u00b0 \u2014 \u6642\u8a08\u56de\u308a\uff08\u53f3\u7aef\u304c\u4e0b\uff09",
        "CalDirCCW": "270\u00b0 \u2014 \u53cd\u6642\u8a08\u56de\u308a\uff08\u5de6\u7aef\u304c\u4e0b\uff09",
        "CalStep3": "\u624b\u9806 3/3 \u2014 \u300c\u56de\u8ee2\u3092\u4fdd\u5b58\u300d\u3092\u62bc\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "CalSaveRot": "\u56de\u8ee2\u3092\u4fdd\u5b58",
        "CalVerify": "\u691c\u51fa\u3057\u305f\u56de\u8ee2\uff1a{0}\u00b0\uff08\u4e88\u60f3 {1}\u00b0\uff09",
        "CalMismatch": "\u4e0d\u4e00\u81f4 \u2014 \u691c\u51fa {0}\u00b0\u3001\u4e88\u60f3 {1}\u00b0\u3002\u65b9\u5411\u3092\u78ba\u8a8d\u3057\u3066\u3084\u308a\u76f4\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "EnableWallpaper": "\u56de\u8ee2\u6642\u306b\u58c1\u7d19\u3092\u5909\u66f4",
        "RotMonHeader": "\u56de\u8ee2\u30e2\u30cb\u30bf\u30fc",
        "ChangeRestWallpaper": "\u4ed6\u306e\u30e2\u30cb\u30bf\u30fc\u306e\u58c1\u7d19\u3092\u5909\u66f4",
        "FollowRotWallpaper": "\u56de\u8ee2\u30e2\u30cb\u30bf\u30fc\u306e\u58c1\u7d19\u306b\u5408\u308f\u305b\u308b\uff08\u5207\u308a\u53d6\u308a\u306e\u53ef\u80fd\u6027\u3042\u308a\uff09",
        "RestLandscapeLabel": "\u4ed6\u306e\u30e2\u30cb\u30bf\u30fc \u2014 \u56de\u8ee2\u30e2\u30cb\u30bf\u30fc\u304c\u6a2a\u5411\u304d\u306e\u6642\uff1a",
        "RestPortraitLabel": "\u4ed6\u306e\u30e2\u30cb\u30bf\u30fc \u2014 \u56de\u8ee2\u30e2\u30cb\u30bf\u30fc\u304c\u7e26\u5411\u304d\u306e\u6642\uff1a",
        "MsgWallpaperDisabled": "\u58c1\u7d19\u306e\u5909\u66f4\u306f\u7121\u52b9\u3067\u3059\u3002",
        "SaveWallpaper": "\u58c1\u7d19\u3092\u4fdd\u5b58",
        "MsgSaved": "\u4fdd\u5b58\u3057\u307e\u3057\u305f",
        "CalNotConnected": "\u672a\u63a5\u7d9a \u2014 \u5148\u306bCOM\u30dd\u30fc\u30c8\u3078\u63a5\u7d9a\u3057\u3066\u304f\u3060\u3055\u3044\u3002",
        "AboutFirmware": "\u30d5\u30a1\u30fc\u30e0\u30a6\u30a7\u30a2\u30ea\u30dd\u30b8\u30c8\u30ea",
        "ViewLicenses": "\u30b5\u30fc\u30c9\u30d1\u30fc\u30c6\u30a3\u30fc\u30e9\u30a4\u30bb\u30f3\u30b9",
        "SilentStartWithWindows": "\u30d0\u30c3\u30af\u30b0\u30e9\u30a6\u30f3\u30c9\u3067\u9759\u304b\u306b\u8d77\u52d5\uff08\u7a93\u3092\u8868\u793a\u3057\u306a\u3044\uff09",
        "AutoStartMonitor": "\u63a5\u7d9a\u5f8c\u306b\u76e3\u8996\u3092\u81ea\u52d5\u958b\u59cb",
        "CheckUpdatesOnStartup": "\u8d77\u52d5\u6642\u306b\u66f4\u65b0\u3092\u78ba\u8a8d",
        "AboutTagline": "\u4e00\u56de\u8ee2\u3067\u63c3\u3046\u3002",
        "DownloadUpdate": "\u66f4\u65b0\u3092\u30c0\u30a6\u30f3\u30ed\u30fc\u30c9",
        "DownloadingUpdate": "\u30c0\u30a6\u30f3\u30ed\u30fc\u30c9\u4e2d\u2026",
        "MsgUpdateReadyTitle": "\u66f4\u65b0\u306e\u6e96\u5099\u5b8c\u4e86",
        "MsgUpdateReadyBody": "\u66f4\u65b0\u306f {0} \u306b\u30c0\u30a6\u30f3\u30ed\u30fc\u30c9\u3055\u308c\u307e\u3057\u305f\u3002Rotary \u3092\u7d42\u4e86\u3057\u3066\u30a4\u30f3\u30b9\u30c8\u30fc\u30e9\u30fc\u3092\u8d77\u52d5\u3057\u307e\u3059\u304b\uff1f",
        "MsgInstallNow": "\u4eca\u3059\u3050\u30a4\u30f3\u30b9\u30c8\u30fc\u30eb",
        "MsgUpdateDownloadFailed": "\u66f4\u65b0\u306e\u30c0\u30a6\u30f3\u30ed\u30fc\u30c9\u306b\u5931\u6557\u3057\u307e\u3057\u305f\uff1a{0}",
        "MsgAutoStartMonitor": "\u76e3\u8996\u3092\u81ea\u52d5\u958b\u59cb\u3057\u307e\u3057\u305f\u3002",
    },
}


def main():
    langs, order = load()
    for lang in order:
        langs[lang].update(NEW[lang])
    emit_cs(langs, order)
    for lang in order:
        emit_resw(lang, langs[lang])
    print("regenerated Localization.cs + resw;", sum(len(v) for v in langs.values()), "entries")


main()
