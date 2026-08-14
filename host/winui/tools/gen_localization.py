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
        "TrayShow": "Show",
        "TrayExit": "Exit",
        "TrayHint": "Rotary is still running in the tray.",
    },
    "zh-CN": {
        "AppTitle.Title": "Rotary",
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
        "TrayShow": "\u663e\u793a",
        "TrayExit": "\u9000\u51fa",
        "TrayHint": "Rotary \u4ecd\u5728\u6258\u76d8\u8fd0\u884c\u4e2d\u3002",
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
        "TrayShow": "\u8868\u793a",
        "TrayExit": "\u7d42\u4e86",
        "TrayHint": "Rotary \u306f\u30c8\u30ec\u30a4\u3067\u5b9f\u884c\u4e2d\u3067\u3059\u3002",
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
