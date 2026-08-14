# Rotary

Windows software for the **Rotary** monitor-rotation accessory. It connects to a
D1 Mini + BMI160 sensor (see the [rotary-firmware](https://github.com/nonefffds/rotary-firmware)
repo) over USB serial, and when you rotate your monitor it automatically:

- rotates the chosen display to match (0°/90°/180°/270°), and
- swaps the wallpaper — each monitor gets the landscape or portrait image that
  matches its own orientation.

The app is a **WinUI 3** desktop app (Fluent UI), fully self-contained
(Windows App SDK + .NET 8 bundled), with en-US / 简体中文 / 日本語 UI,
a one-time **mounting calibration**, system-tray support, auto-start with
Windows, auto-restart on crash, and update checks against GitHub releases.

```
[D1 Mini + BMI160 on back of monitor]
              |
         USB serial (COM port)
              |
   host/winui/RotaryMonitor.exe
              |
      +-------+--------+
      |                 |
  rotate display    swap wallpaper
  (0/90/180/270)
```

## Using it

1. Flash the firmware (see the **rotary-firmware** repo) and attach the sensor
   to your monitor.
2. Get the app:
   - **Installer**: `host/winui/installer/output/RotarySetup.exe` — per-user
     install (no admin), Start-menu + desktop shortcuts, uninstaller.
     Rebuild any time with `host/winui/installer/build-installer.bat`.
   - **Portable folder**: run `host/winui/publish.bat` → the `dist/` folder is
     fully self-contained: copy it to any x64 Windows 10/11, no install needed
     (only the CH340 driver).
   - Note: WinUI 3 does not support single-file publish, so the self-contained
     folder (or the installer) is the distribution method.
3. In the app:
   - **Monitor** → pick the COM port, **Connect**, then **Start monitoring**.
   - **Calibrate…** once — keep the monitor horizontal, choose 90° or 270°,
     rotate it, press **Capture orientation**.
   - **Wallpaper** → set landscape/portrait images and the scope
     (all monitors, or only the rotation monitor).
   - **Options** → auto-connect, start with Windows, auto-restart on crash,
     language (defaults to your system language).
   - The tray icon keeps the app running; close minimizes to the tray.

## Repository layout

- `host/winui/` — the WinUI 3 app (project + `publish.bat` + `dist/` output).
  - `installer/` — Inno Setup script + `build-installer.bat` (`RotarySetup.exe`).
  - `Assets/rotary.ico` — app / tray icon (source: `Assets/rotary.png`).
  - `Strings/{en-US,zh-CN,ja-JP}/Resources.resw` + `tools/gen_localization.py`
    — localized strings (regenerate with `python tools/gen_localization.py`).
  - `MainWindow.xaml(.cs)` — sidebar UI, serial/monitoring, calibration,
    wallpaper, options, about.
  - `TrayIcon.cs`, `NativeMethods.cs` — system tray + Win32 interop.
  - `Win32.cs` — display rotation / wallpaper P/Invoke.
  - `Wallpaper.cs` — per-monitor wallpaper composition.
  - `SensorWindow.xaml(.cs)` — read-only live angle view.

## Building

Requires the .NET 8 SDK (e.g. on `D:\dotnet`) and NuGet packages on
`D:\nuget`:

```
cd host\winui
publish.bat        # builds the self-contained dist\ folder
```

## Update checks

The About page checks the latest release against this GitHub repository's
releases. Tag releases with the app version (e.g. `v1.0.0`) to enable it.
