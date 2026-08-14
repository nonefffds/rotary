# Rotary

Windows software for the **Rotary** monitor-rotation accessory. It connects to a
D1 Mini + BMI160 sensor (see the [rotary-firmware](https://github.com/nonefffds/rotary-firmware)
repo) over USB serial, and when you rotate your monitor it automatically:

- rotates the chosen display to match (0°/90°/180°/270°), and
- swaps wallpapers — the rotation monitor gets a landscape or portrait image
  matching its orientation, while the other monitors can keep their own
  wallpaper, follow the rotated monitor (may crop), or use a separate
  landscape/portrait pair.

The app is a **WinUI 3** desktop app (Fluent UI), fully self-contained
(Windows App SDK + .NET 8 bundled), with en-US / 简体中文 / 日本語 UI
(defaults to your system language), a 3-step **mounting calibration**, system
tray, auto-start with Windows, auto-restart on crash, and update checks against
GitHub releases.

```
[D1 Mini + BMI160 on back of monitor]
              |
         USB serial (COM port)
              |
   RotaryMonitor.exe (WinUI 3)
              |
      +-------+--------+
      |                 |
  rotate display    swap wallpaper
  (0/90/180/270)
```

## Download

Grab the latest build from the [Releases](https://github.com/nonefffds/rotary/releases) page:

- **RotarySetup.exe** — per-user installer (no admin; Start-menu + desktop
  shortcuts, uninstaller).
- **RotaryPortable.zip** — self-contained portable folder: extract and run,
  no install needed (only the CH340 driver on the target PC).

Pre-built binaries are not kept in the repo.

## Using it

1. Flash the firmware (see the **rotary-firmware** repo) and attach the sensor
   to your monitor.
2. Install / extract the app, then:
   - **Monitor** → pick the COM port, **Connect**, then **Start monitoring**.
   - **Calibrate…** once (3 steps): keep the monitor horizontal → **Save
     baseline**; choose **90° (clockwise)** or **270° (counterclockwise)**;
     rotate the monitor to that position → **Save rotation**. The app stores the
     mounting offset and verifies the measured rotation.
   - **Wallpaper** → enable *Change wallpaper when rotated*, set the rotation
     monitor's landscape/portrait images, then optionally change the other
     monitors too — either follow the rotation monitor's wallpaper (may crop)
     or set a separate pair for them.
   - **Options** → auto-connect, start with Windows, auto-restart on crash,
     language.
   - **About** → firmware repo link, third-party licenses, check for updates.
   - The **Exit** button (top-right) closes the app; the **X** minimizes to the
     tray (right-click tray icon → Show / Exit).

## Repository layout

- `host/winui/` — the WinUI 3 app.
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
  - `THIRD-PARTY-NOTICES.txt` — open-source attributions (shown in the app).

## Building

Requires the .NET 8 SDK (e.g. on `D:\dotnet`) and NuGet packages on
`D:\nuget`:

```
cd host\winui
publish.bat               # builds the self-contained dist\ folder
installer\build-installer.bat   # builds installer\output\RotarySetup.exe
```

## Update checks

The About page compares the app version against the latest release on this
repo. Tag releases with the app version (e.g. `v1.0.1`) to enable it.

## License

[MIT](LICENSE). Third-party notices in `THIRD-PARTY-NOTICES.txt`. The firmware
repo is [MIT](https://github.com/nonefffds/rotary-firmware/blob/main/LICENSE)
with an LGPL-2.1 note for the ESP8266 Arduino core.
