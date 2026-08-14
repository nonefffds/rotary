using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Globalization;
using Windows.Graphics;
using Windows.Storage.Pickers;
using Windows.UI;

namespace RotaryMonitor
{
    public sealed partial class MainWindow : Window
    {
        private readonly AppConfig _cfg;
        private SerialPort? _serial;
        private Thread? _readerThread;
        private bool _readerDone;
        private double _latestAngle = double.NaN;
        private volatile bool _wantRun;
        private volatile int _lastApplied = -1;
        private bool _firstMessage = true;
        private string _savedLanguage = "";
        private bool _startupInit;
        private SensorWindow? _sensorWindow;
        private TrayIcon? _tray;
        private bool _trayHintShown;

        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunValueName = "RotaryMonitor";
        private const string RepoUrl = "https://github.com/nonefffds/rotary";

        public MainWindow()
        {
            _cfg = App.Config;
            InitializeComponent();
            Title = L.Get("AppTitle.Title");
            string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "rotary.ico");
            if (File.Exists(iconPath))
            {
                try { AppWindow.SetIcon(iconPath); } catch { }
            }
            try
            {
                string logoPath = Path.Combine(AppContext.BaseDirectory, "Assets", "rotary.png");
                if (File.Exists(logoPath))
                    LogoImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(logoPath));
            }
            catch { }
            AppWindow.Resize(new SizeInt32(920, 920));
            LoadConfigIntoUi();
            try { SetupTray(); } catch { }
            AppWindow.Closing += OnAppWindowClosing;
            Closed += (s, e) =>
            {
                SaveConfig();
                _tray?.Dispose();
            };
        }

        // ---------------- UI init ----------------

        private void LocalizeUi()
        {
            TitleText.Text = L.Get("AppTitle.Title");
            ExitButtonText.Text = L.Get("ExitButton");

            NavMonitor.Content = L.Get("NavMonitor");
            NavWallpaper.Content = L.Get("NavWallpaper");
            NavOptions.Content = L.Get("NavOptions");
            NavLog.Content = L.Get("NavLog");
            NavAbout.Content = L.Get("NavAbout");

            SerialHeaderText.Text = L.Get("SerialHeader.Text");
            PortLabelText.Text = L.Get("PortLabel.Text");
            RefreshButtonText.Text = L.Get("RefreshButton.Content");
            ConnectButtonText.Text = L.Get("PortConnect");
            StartButtonText.Text = L.Get("StartButton.Content");
            SensorButtonText.Text = L.Get("SensorButton.Content");

            MappingHeaderText.Text = L.Get("MappingHeader.Text");
            RotateTargetLabelText.Text = L.Get("RotateTargetLabel");
            CalibrateButtonText.Text = L.Get("CalButton");
            MappingHintText.Text = L.Get("MappingHint");

            TestHeaderText.Text = L.Get("TestHeader.Text");
            TestLandscapeButton.Content = L.Get("TestLandscapeButton.Content");
            TestPortrait90Button.Content = L.Get("TestPortrait90Button.Content");
            TestFlippedButton.Content = L.Get("TestFlippedButton.Content");
            TestPortrait270Button.Content = L.Get("TestPortrait270Button.Content");

            WallpaperHeaderText.Text = L.Get("WallpaperHeader.Text");
            EnableWallpaperToggle.Header = L.Get("EnableWallpaper");
            RotMonHeaderText.Text = L.Get("RotMonHeader");
            LandscapeLabelText.Text = L.Get("LandscapeLabel.Text");
            PortraitLabelText.Text = L.Get("PortraitLabel.Text");
            ChangeRestToggle.Header = L.Get("ChangeRestWallpaper");
            FollowRotToggle.Header = L.Get("FollowRotWallpaper");
            RestLandscapeLabelText.Text = L.Get("RestLandscapeLabel");
            RestPortraitLabelText.Text = L.Get("RestPortraitLabel");

            OptionsHeaderText.Text = L.Get("OptionsHeader");
            ApplyOnStartupCheck.Content = L.Get("ApplyOnStartupCheck.Content");
            StartWithWindowsCheck.Content = L.Get("StartWithWindows");
            AutoConnectCheck.Content = L.Get("AutoConnect");
            AutoRestartCheck.Content = L.Get("AutoRestart");
            LanguageLabelText.Text = L.Get("LanguageLabel.Text");
            SaveButtonText.Text = L.Get("SaveButton.Content");
            RestartButtonText.Text = L.Get("RestartNow");

            LogHeaderText.Text = L.Get("LogHeader.Text");

            AboutHeaderText.Text = L.Get("NavAbout");
            AboutVersionText.Text = string.Format(L.Get("AboutVersion"), AppVersion);
            AboutRepoLink.Content = L.Get("AboutRepo");
            AboutFirmwareLink.Content = L.Get("AboutFirmware");
            CheckUpdatesButtonText.Text = L.Get("CheckUpdates");
            LicensesButtonText.Text = L.Get("ViewLicenses");

            LanguageCombo.Items.Clear();
            LanguageCombo.Items.Add(L.Get("LangSystem"));
            LanguageCombo.Items.Add("English (en-US)");
            LanguageCombo.Items.Add("\u4E2D\u6587 (\u7B80\u4F53)");
            LanguageCombo.Items.Add("\u65E5\u672C\u8A9E");
        }

        private void LoadConfigIntoUi()
        {
            LocalizeUi();

            ApplyOnStartupCheck.IsChecked = _cfg.ApplyOnStartup;
            EnableWallpaperToggle.IsOn = _cfg.EnableWallpaper;
            ChangeRestToggle.IsOn = _cfg.ChangeRestWallpaper;
            FollowRotToggle.IsOn = _cfg.RestFollowRotation;
            LandscapeBox.Text = _cfg.LandscapeWallpaper;
            PortraitBox.Text = _cfg.PortraitWallpaper;
            RestLandscapeBox.Text = _cfg.RestLandscapeWallpaper;
            RestPortraitBox.Text = _cfg.RestPortraitWallpaper;
            UpdateWallpaperVisibility();

            UpdateCalStatus();

            int li = 0;
            switch (_cfg.Language)
            {
                case "en-US": li = 1; break;
                case "zh-CN": li = 2; break;
                case "ja-JP": li = 3; break;
            }
            LanguageCombo.SelectedIndex = li;
            _savedLanguage = _cfg.Language;

            _startupInit = true;
            StartWithWindowsCheck.IsChecked = _cfg.StartWithWindows;
            ApplyStartupRegistry(_cfg.StartWithWindows);
            AutoConnectCheck.IsChecked = _cfg.AutoConnect;
            AutoRestartCheck.IsChecked = _cfg.AutoRestart;
            ApplyAutoRestartRegistration(_cfg.AutoRestart);
            _startupInit = false;

            RefreshPorts();
            RefreshMonitors();

            Nav.SelectedItem = NavMonitor;

            if (_cfg.AutoConnect)
                DispatcherQueue.TryEnqueue(delegate { Connect(); });
        }

        private void ReadConfigFromUi()
        {
            _cfg.ComPort = SelectedPort();
            _cfg.ApplyOnStartup = ApplyOnStartupCheck.IsChecked == true;
            _cfg.EnableWallpaper = EnableWallpaperToggle.IsOn;
            _cfg.ChangeRestWallpaper = ChangeRestToggle.IsOn;
            _cfg.RestFollowRotation = FollowRotToggle.IsOn;
            _cfg.LandscapeWallpaper = LandscapeBox.Text.Trim();
            _cfg.PortraitWallpaper = PortraitBox.Text.Trim();
            _cfg.RestLandscapeWallpaper = RestLandscapeBox.Text.Trim();
            _cfg.RestPortraitWallpaper = RestPortraitBox.Text.Trim();
            _cfg.Language = LangFromIndex(LanguageCombo.SelectedIndex);
            _cfg.StartWithWindows = StartWithWindowsCheck.IsChecked == true;
            _cfg.AutoConnect = AutoConnectCheck.IsChecked == true;
            _cfg.AutoRestart = AutoRestartCheck.IsChecked == true;
            _cfg.RotateMonitor = (RotateTargetCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "";
        }

        private void SaveConfig()
        {
            ReadConfigFromUi();
            _cfg.Save(AppConfig.DefaultPath);
            Log(string.Format(L.Get("MsgSettingsSaved"), AppConfig.DefaultPath));
        }

        private static int Clamp(int v, int lo, int hi)
        {
            return v < lo ? lo : (v > hi ? hi : v);
        }

        private static string LangFromIndex(int i)
        {
            switch (i)
            {
                case 1: return "en-US";
                case 2: return "zh-CN";
                case 3: return "ja-JP";
                default: return "";
            }
        }

        /// <summary>Port currently shown in the ComboBox. Uses SelectedItem because
        /// ComboBox.Text does not reliably reflect the selection in WinUI 3.</summary>
        private string SelectedPort()
        {
            if (PortCombo.SelectedItem != null)
                return PortCombo.SelectedItem.ToString() ?? "";
            string text = PortCombo.Text;
            return string.IsNullOrEmpty(text) ? "" : text.Trim();
        }

        private void RefreshPorts()
        {
            PortCombo.Items.Clear();
            var names = new List<string>(SerialPort.GetPortNames());
            names.Sort(delegate(string a, string b)
            {
                return ParsePortNumber(b).CompareTo(ParsePortNumber(a));
            });
            foreach (string n in names)
                PortCombo.Items.Add(n);
            if (names.Count > 0)
            {
                if (!string.IsNullOrEmpty(_cfg.ComPort) && names.Contains(_cfg.ComPort))
                    PortCombo.SelectedItem = _cfg.ComPort;
                else
                    PortCombo.SelectedIndex = 0;
            }
            Log(string.Format(L.Get("MsgPorts"), names.Count == 0 ? "-" : string.Join(", ", names)));
        }

        private static int ParsePortNumber(string port)
        {
            int i = port.IndexOf("COM", StringComparison.OrdinalIgnoreCase);
            int n;
            return (i >= 0 && int.TryParse(port.Substring(i + 3), out n)) ? n : -1;
        }

        private void RefreshMonitors()
        {
            RotateTargetCombo.Items.Clear();
            List<Win32.MonitorInfo> monitors = Win32.EnumMonitors();
            string primary = "";
            foreach (var m in monitors)
            {
                string label = m.DeviceName + " \u00B7 "
                    + (m.Rect.Right - m.Rect.Left) + "x" + (m.Rect.Bottom - m.Rect.Top);
                if (m.IsPrimary)
                {
                    primary = m.DeviceName;
                    label += " (" + L.Get("PrimaryMonitor") + ")";
                }
                var item = new ComboBoxItem { Content = label, Tag = m.DeviceName };
                RotateTargetCombo.Items.Add(item);
            }

            string target = _cfg.RotateMonitor;
            if (string.IsNullOrEmpty(target))
                target = primary;
            for (int i = 0; i < RotateTargetCombo.Items.Count; i++)
            {
                if (((ComboBoxItem)RotateTargetCombo.Items[i]).Tag as string == target)
                {
                    RotateTargetCombo.SelectedIndex = i;
                    break;
                }
            }
            if (RotateTargetCombo.SelectedIndex < 0 && RotateTargetCombo.Items.Count > 0)
                RotateTargetCombo.SelectedIndex = 0;
        }

        private string ResolveRotateTarget()
        {
            if (!string.IsNullOrEmpty(_cfg.RotateMonitor))
                return _cfg.RotateMonitor;
            return Win32.PrimaryDeviceName();
        }

        // ---------------- Navigation ----------------

        private void OnNavSelection(object sender, NavigationViewSelectionChangedEventArgs e)
        {
            string tag = (Nav.SelectedItem as NavigationViewItem)?.Tag as string ?? "monitor";
            bool monitor = tag == "monitor";
            bool wallpaper = tag == "wallpaper";
            bool options = tag == "options";
            bool log = tag == "log";
            bool about = tag == "about";

            MonitorScroll.Visibility = monitor ? Visibility.Visible : Visibility.Collapsed;
            WallpaperScroll.Visibility = wallpaper ? Visibility.Visible : Visibility.Collapsed;
            OptionsScroll.Visibility = options ? Visibility.Visible : Visibility.Collapsed;
            LogScroll.Visibility = log ? Visibility.Visible : Visibility.Collapsed;
            AboutScroll.Visibility = about ? Visibility.Visible : Visibility.Collapsed;
        }

        // ---------------- Start with Windows / auto-restart ----------------

        private static bool IsStartupEnabled()
        {
            using (var k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RunKeyPath))
                return k?.GetValue(RunValueName) != null;
        }

        private static void ApplyStartupRegistry(bool enabled)
        {
            using (var k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RunKeyPath, true))
            {
                if (enabled)
                    k.SetValue(RunValueName, "\"" + Environment.ProcessPath + "\"");
                else
                    k.DeleteValue(RunValueName, false);
            }
        }

        private void OnStartupChanged(object sender, RoutedEventArgs e)
        {
            if (_startupInit)
                return;
            bool enabled = StartWithWindowsCheck.IsChecked == true;
            _cfg.StartWithWindows = enabled;
            ApplyStartupRegistry(enabled);
            _cfg.Save(AppConfig.DefaultPath);
            SetStatusInfo(enabled ? L.Get("MsgStartupOn") : L.Get("MsgStartupOff"),
                InfoBarSeverity.Success);
        }

        private static void ApplyAutoRestartRegistration(bool enabled)
        {
            try
            {
                if (enabled)
                {
                    int hr = Win32.RegisterApplicationRestart("\"" + Environment.ProcessPath + "\"", 0);
                    if (hr != 0)
                        Debug.WriteLine("RegisterApplicationRestart failed hr=0x" + hr.ToString("X"));
                }
                else
                {
                    Win32.UnregisterApplicationRestart();
                }
            }
            catch { }
        }

        private void OnAutoConnectChanged(object sender, RoutedEventArgs e)
        {
            if (_startupInit)
                return;
            _cfg.AutoConnect = AutoConnectCheck.IsChecked == true;
            _cfg.Save(AppConfig.DefaultPath);
        }

        private void OnAutoRestartChanged(object sender, RoutedEventArgs e)
        {
            if (_startupInit)
                return;
            bool enabled = AutoRestartCheck.IsChecked == true;
            _cfg.AutoRestart = enabled;
            ApplyAutoRestartRegistration(enabled);
            _cfg.Save(AppConfig.DefaultPath);
            SetStatusInfo(enabled ? L.Get("MsgAutoRestartOn") : L.Get("MsgAutoRestartOff"),
                InfoBarSeverity.Success);
        }

        private void OnRestartClick(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? "RotaryMonitor.exe")
            {
                UseShellExecute = true,
                WorkingDirectory = AppContext.BaseDirectory,
            });
            Application.Current.Exit();
        }

        // ---------------- Events ----------------

        private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshPorts();

        private void OnStartClick(object sender, RoutedEventArgs e) => ToggleStart();

        private void OnSensorClick(object sender, RoutedEventArgs e) => OpenSensorView();

        private void OnTest0(object sender, RoutedEventArgs e) => ApplyAndLog(0);
        private void OnTest90(object sender, RoutedEventArgs e) => ApplyAndLog(1);
        private void OnTest180(object sender, RoutedEventArgs e) => ApplyAndLog(2);
        private void OnTest270(object sender, RoutedEventArgs e) => ApplyAndLog(3);

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            string old = _savedLanguage;
            SaveConfig();
            _savedLanguage = _cfg.Language;
            SetStatusInfo(L.Get("MsgSettingsSaved"), InfoBarSeverity.Success);

            if (_cfg.Language != old)
            {
                _ = ConfirmRestartAsync();
            }
        }

        private async Task ConfirmRestartAsync()
        {
            var dlg = new ContentDialog
            {
                Title = L.Get("MsgLanguageChangedTitle"),
                Content = L.Get("MsgLanguageChangedBody"),
                PrimaryButtonText = L.Get("MsgRestart"),
                CloseButtonText = L.Get("MsgCancel"),
                XamlRoot = Content.XamlRoot,
            };
            if (await dlg.ShowAsync() == ContentDialogResult.Primary)
            {
                Process.Start(new ProcessStartInfo(Environment.ProcessPath ?? "RotaryMonitor.exe")
                {
                    UseShellExecute = true,
                    WorkingDirectory = AppContext.BaseDirectory,
                });
                Application.Current.Exit();
            }
        }

        private void OnBrowseLandscape(object sender, RoutedEventArgs e) => BrowseImage(LandscapeBox);
        private void OnBrowsePortrait(object sender, RoutedEventArgs e) => BrowseImage(PortraitBox);
        private void OnBrowseRestLandscape(object sender, RoutedEventArgs e) => BrowseImage(RestLandscapeBox);
        private void OnBrowseRestPortrait(object sender, RoutedEventArgs e) => BrowseImage(RestPortraitBox);

        private async void BrowseImage(TextBox target)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            };
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".bmp");
            WinRT.Interop.InitializeWithWindow.Initialize(picker,
                WinRT.Interop.WindowNative.GetWindowHandle(this));
            var file = await picker.PickSingleFileAsync();
            if (file != null)
                target.Text = file.Path;
        }

        private void OnWallpaperToggle(object sender, RoutedEventArgs e)
        {
            UpdateWallpaperVisibility();
        }

        private void UpdateWallpaperVisibility()
        {
            WallpaperOptions.Visibility = EnableWallpaperToggle.IsOn
                ? Visibility.Visible : Visibility.Collapsed;
            RestOptions.Visibility = (EnableWallpaperToggle.IsOn && ChangeRestToggle.IsOn)
                ? Visibility.Visible : Visibility.Collapsed;
            RestPickers.Visibility = (EnableWallpaperToggle.IsOn && ChangeRestToggle.IsOn && !FollowRotToggle.IsOn)
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            // applied on Save; the UI is already loaded
        }

        // ---------------- Monitoring ----------------

        private void SetStartButton(bool running)
        {
            StartButtonText.Text = running ? L.Get("PortStop") : L.Get("PortStart");
            StartGlyph.Glyph = running ? "\uE71A" : "\uE768";
        }

        private bool IsConnected => _serial != null && _serial.IsOpen;

        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (IsConnected)
                Disconnect();
            else
                Connect();
        }

        private void Connect()
        {
            if (_sensorWindow != null)
            {
                SetStatusInfo(L.Get("MsgCloseSensorFirst"), InfoBarSeverity.Warning);
                return;
            }
            ReadConfigFromUi();
            string port = _cfg.ComPort;
            if (string.IsNullOrEmpty(port))
            {
                var ports = SerialPort.GetPortNames();
                if (ports != null && ports.Length > 0)
                {
                    port = ports[0];
                    _cfg.ComPort = port;
                    PortCombo.SelectedItem = port;
                    Log(string.Format(L.Get("MsgAutoPickedPort"), port));
                }
            }
            if (string.IsNullOrEmpty(port))
            {
                SetStatusInfo(L.Get("MsgPickPortFirst"), InfoBarSeverity.Warning);
                return;
            }
            try
            {
                var sp = new SerialPort(port, 115200);
                sp.ReadTimeout = 2000;
                sp.Open();
                _serial = sp;
                _readerDone = false;
                _readerThread = new Thread(ReaderLoop) { IsBackground = true };
                _readerThread.Start();
                Log(string.Format(L.Get("MsgSerialOpen"), port));
                SetStatusInfo(string.Format(L.Get("StatusConnected"), port), InfoBarSeverity.Success);
                DispatcherQueue.TryEnqueue(delegate
                {
                    ConnectButtonText.Text = L.Get("PortDisconnect");
                    StartButton.IsEnabled = true;
                    _sensorWindow?.SetPort(port);
                    _sensorWindow?.SetConnected(true);
                });
            }
            catch (Exception ex)
            {
                Log(string.Format(L.Get("MsgSerialError"), ex.Message));
                SetStatusInfo(L.Get("StatusDisconnected"), InfoBarSeverity.Warning);
                _serial = null;
            }
        }

        private void Disconnect()
        {
            _wantRun = false;
            _readerDone = true;
            if (_readerThread != null)
                _readerThread.Join(2000);
            try { _serial?.Close(); } catch { }
            _serial = null;
            SetStatusInfo(L.Get("StatusDisconnected"), InfoBarSeverity.Informational);
            DispatcherQueue.TryEnqueue(delegate
            {
                ConnectButtonText.Text = L.Get("PortConnect");
                SetStartButton(false);
                StartButton.IsEnabled = false;
                _sensorWindow?.SetConnected(false);
            });
        }

        private void ToggleStart()
        {
            if (_wantRun)
            {
                _wantRun = false;
                StartButton.IsEnabled = false;
                SetStartButton(false);
                Log(L.Get("MsgStopping"));
                return;
            }
            if (_sensorWindow != null)
            {
                SetStatusInfo(L.Get("MsgCloseSensorFirst"), InfoBarSeverity.Warning);
                return;
            }
            if (!IsConnected)
            {
                SetStatusInfo(L.Get("MsgConnectFirst"), InfoBarSeverity.Warning);
                return;
            }
            _firstMessage = true;
            _lastApplied = -1;
            _wantRun = true;
            SetStartButton(true);
        }

        /// <summary>Map a measured angle (with mounting offset applied) to a
        /// Windows rotation: 0 = upright, 1 = 90°, 2 = 180°, 3 = 270°.
        /// Returns -1 inside the dead band (keep the previous rotation).</summary>
        private static int AngleToRotation(double r)
        {
            if (r >= -30 && r <= 30) return 0;
            if (r >= 60 && r <= 120) return 1;
            if (r >= 150 || r <= -150) return 2;
            if (r >= -120 && r <= -60) return 3;
            return -1;
        }

        private static double NormalizeAngle(double a)
        {
            while (a > 180) a -= 360;
            while (a < -180) a += 360;
            return a;
        }

        private static bool TryParseReading(string line, out double angle, out float ax, out float ay, out float az)
        {
            angle = 0; ax = 0; ay = 0; az = 0;
            int eq = line.IndexOf('=');
            if (eq < 0) return false;
            string[] p = line.Substring(eq + 1).Trim().Split(' ');
            if (p.Length < 4) return false;
            return double.TryParse(p[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out angle)
                && float.TryParse(p[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out ax)
                && float.TryParse(p[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out ay)
                && float.TryParse(p[3], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out az);
        }

        private void ProcessAngle(double angle)
        {
            double r = NormalizeAngle(angle - _cfg.MountOffset);
            int rot = AngleToRotation(r);
            if (rot < 0) return;                      // in the dead band
            if (rot == _lastApplied) return;
            if (_firstMessage)
            {
                _firstMessage = false;
                _lastApplied = rot;
                if (!_cfg.ApplyOnStartup)
                {
                    Log(string.Format(L.Get("MsgBaseline"), rot));
                    return;
                }
            }
            _lastApplied = rot;
            ApplyAndLog(rot);
        }

        private void UpdateCalStatus()
        {
            if (_cfg.MountOffset == 0)
                CalStatusText.Text = L.Get("CalNotCalibrated");
            else
                CalStatusText.Text = string.Format(L.Get("CalStatus"),
                    _cfg.MountOffset.ToString("0.#",
                        System.Globalization.CultureInfo.InvariantCulture));
        }

        private void ReaderLoop()
        {
            bool unexpected = false;
            try
            {
                while (!_readerDone)
                {
                    SerialPort sp = _serial;
                    if (sp == null || !sp.IsOpen)
                    {
                        unexpected = true;
                        break;
                    }
                    string line;
                    try { line = sp.ReadLine(); }
                    catch (TimeoutException) { continue; }
                    catch { unexpected = true; break; }
                    if (line == null) continue;
                    line = line.Trim();
                    if (line.Length == 0) continue;
                    if (line.StartsWith("A="))
                    {
                        double angle;
                        float ax, ay, az;
                        if (TryParseReading(line, out angle, out ax, out ay, out az))
                        {
                            _latestAngle = angle;
                            if (_wantRun)
                                ProcessAngle(angle);
                            _sensorWindow?.OnReading(angle, ax, ay, az);
                        }
                    }
                    else
                    {
                        LogRx(line);
                    }
                }
            }
            finally
            {
                if (unexpected && !_readerDone)
                {
                    try { _serial?.Close(); } catch { }
                    _serial = null;
                    SetStatusInfo(L.Get("StatusDisconnected"), InfoBarSeverity.Warning);
                    DispatcherQueue.TryEnqueue(delegate
                    {
                        ConnectButtonText.Text = L.Get("PortConnect");
                        SetStartButton(false);
                        StartButton.IsEnabled = false;
                        _sensorWindow?.SetConnected(false);
                    });
                }
            }
        }

        private void OpenSensorView()
        {
            if (_sensorWindow != null)
            {
                _sensorWindow.Activate();
                return;
            }
            if (!IsConnected)
                Connect();
            if (!IsConnected)
                return;

            var sensor = new SensorWindow();
            _sensorWindow = sensor;
            sensor.SetPort(_cfg.ComPort);
            sensor.SetConnected(true);
            sensor.Closed += (s, e) => _sensorWindow = null;
            sensor.Activate();
            Log(string.Format(L.Get("MsgOpenSensor"), _cfg.ComPort));
        }

        // ---------------- Calibration ----------------

        private async void OnCalibrateClick(object sender, RoutedEventArgs e)
        {
            if (_sensorWindow != null)
            {
                _sensorWindow.Close();
                _sensorWindow = null;
            }
            ReadConfigFromUi();
            if (!IsConnected)
                Connect();
            if (!IsConnected)
                return;
            await RunCalibrationAsync();
        }

        private async Task RunCalibrationAsync()
        {
            var panel = new StackPanel { Spacing = 12, MinWidth = 440, MaxWidth = 560 };

            var stepText = new TextBlock { Text = L.Get("CalStep1"), TextWrapping = TextWrapping.Wrap };
            var radio = new RadioButtons();
            radio.Items.Add(L.Get("CalDirCW"));    // 0 -> 90
            radio.Items.Add(L.Get("CalDirCCW"));   // 1 -> 270
            radio.SelectedIndex = 0;
            var liveLabel = new TextBlock
            {
                Text = L.Get("CalLive"),
                Foreground = new SolidColorBrush(UiColors.Gray),
            };
            var live = new TextBlock
            {
                FontSize = 30,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Text = "--\u00B0",
            };
            var save = new Button
            {
                Content = L.Get("CalBaseline"),
                MinWidth = 200,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            var result = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
            };

            panel.Children.Add(stepText);
            panel.Children.Add(radio);
            panel.Children.Add(liveLabel);
            panel.Children.Add(live);
            panel.Children.Add(save);
            panel.Children.Add(result);

            var dlg = new ContentDialog
            {
                Title = L.Get("CalTitle"),
                Content = panel,
                CloseButtonText = L.Get("MsgCancel"),
                PrimaryButtonText = L.Get("CalFinish"),
                IsPrimaryButtonEnabled = false,
                XamlRoot = Content.XamlRoot,
            };

            // live angle readout, fed by the shared connection's latest reading
            var timer = DispatcherQueue.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(200);
            timer.Tick += (s, e) =>
            {
                if (!double.IsNaN(_latestAngle))
                    live.Text = _latestAngle.ToString("0.0",
                        System.Globalization.CultureInfo.InvariantCulture) + "\u00B0";
            };
            timer.Start();

            double baseline = double.NaN;
            save.Click += delegate
            {
                double a = _latestAngle;
                if (double.IsNaN(a))
                {
                    result.Text = L.Get("CalNoData");
                    return;
                }
                if (double.IsNaN(baseline))
                {
                    // Step 1: baseline captured (monitor horizontal)
                    baseline = a;
                    stepText.Text = L.Get("CalStep2");
                    save.Content = L.Get("CalSaveRot");
                    result.Text = string.Format(L.Get("CalBaselineDone"),
                        a.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
                }
                else
                {
                    // Step 3: rotated angle captured -> offset = baseline
                    double chosen = radio.SelectedIndex == 0 ? 90 : 270;
                    double offset = NormalizeAngle(baseline);
                    double delta = NormalizeAngle(a - baseline);
                    _cfg.MountOffset = offset;
                    _cfg.Save(AppConfig.DefaultPath);
                    UpdateCalStatus();
                    result.Text = string.Format(L.Get("CalDone"),
                        offset.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture));
                    if (Math.Abs(delta - chosen) > 30)
                        result.Text += "\n" + string.Format(L.Get("CalMismatch"),
                            delta.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                            chosen.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    else
                        result.Text += "\n" + string.Format(L.Get("CalVerify"),
                            delta.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture),
                            chosen.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    save.IsEnabled = false;
                    dlg.IsPrimaryButtonEnabled = true;
                }
            };

            await dlg.ShowAsync();
            timer.Stop();
        }

        private void ApplyAndLog(int dmdo)
        {
            Log(string.Format(L.Get("MsgApplyingRotation"), dmdo));
            try
            {
                string target = ResolveRotateTarget();
                int res = Win32.SetOrientation(target, (uint)dmdo);
                if (res != Win32.DISP_CHANGE_SUCCESSFUL)
                    Log(string.Format(L.Get("MsgDispChange"), res));
                Thread.Sleep(700);
                ApplyWallpaper(dmdo);
                Log(dmdo == 0 || dmdo == 2
                    ? L.Get("MsgAppliedLandscape")
                    : L.Get("MsgAppliedPortrait"));
            }
            catch (Exception ex)
            {
                Log(string.Format(L.Get("MsgApplyFailed"), ex.Message));
            }
        }

        private void ApplyWallpaper(int dmdo)
        {
            if (!_cfg.EnableWallpaper)
            {
                Log(L.Get("MsgWallpaperDisabled"));
                return;
            }
            string rotLand = _cfg.LandscapeWallpaper;
            string rotPort = _cfg.PortraitWallpaper;
            if (string.IsNullOrEmpty(rotLand) && string.IsNullOrEmpty(rotPort))
            {
                Log(L.Get("MsgWallpaperNotFound"));
                return;
            }
            List<Win32.MonitorInfo> monitors = Win32.EnumMonitors();
            string current = Win32.GetCurrentWallpaper();
            string tmp = Wallpaper.Compose(rotLand, rotPort,
                _cfg.RestLandscapeWallpaper, _cfg.RestPortraitWallpaper, current,
                monitors, ResolveRotateTarget(),
                _cfg.ChangeRestWallpaper, _cfg.RestFollowRotation);
            Win32.SetWallpaper(tmp, "fill");
            Log(string.Format(L.Get("MsgWallpaperBoth"), ResolveRotateTarget()));
        }

        // ---------------- UI marshalling ----------------

        private const int MaxLogLines = 500;
        private readonly List<string> _logLines = new List<string>();

        private void Log(string msg)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                _logLines.Add(DateTime.Now.ToString("HH:mm:ss") + "  " + msg);
                if (_logLines.Count > MaxLogLines)
                    _logLines.RemoveAt(0);
                LogText.Text = string.Join("\n", _logLines);
                LogTextScroll.ChangeView(null, double.MaxValue, null, false);
            });
        }

        private void LogRx(string line)
        {
            // A= lines stream ~6x/s; logging each one floods the UI and makes it
            // unresponsive, so only state changes and other messages are logged.
            if (line.StartsWith("A="))
                return;
            Log("[rx] " + line);
        }

        private void SetStatusInfo(string text, InfoBarSeverity severity)
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                StatusInfo.Title = text;
                StatusInfo.Severity = severity;
                StatusInfo.IsOpen = true;
            });
        }

        // ---------------- Tray ----------------

        private void SetupTray()
        {
            try
            {
                IntPtr hIcon = IntPtr.Zero;
                // Reliable: extract from the exe's embedded icon (the .ico is
                // PNG-compressed, which LoadImageW can render blank/transparent).
                NativeMethods.ExtractIconExW(Environment.ProcessPath ?? "RotaryMonitor.exe",
                    0, out _, out hIcon, 1);
                if (hIcon == IntPtr.Zero)
                {
                    string iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "rotary.ico");
                    if (File.Exists(iconPath))
                        hIcon = NativeMethods.LoadImageW(IntPtr.Zero, iconPath, NativeMethods.IMAGE_ICON,
                            16, 16, NativeMethods.LR_LOADFROMFILE);
                }
                if (hIcon == IntPtr.Zero)
                    hIcon = NativeMethods.LoadImageW(IntPtr.Zero, Environment.ProcessPath ?? "RotaryMonitor.exe",
                        NativeMethods.IMAGE_ICON, 16, 16, 0);
                if (hIcon == IntPtr.Zero)
                    return;

                _tray = new TrayIcon(hIcon, L.Get("AppTitle.Title"));
                _tray.LeftClicked += ShowWindow;
                _tray.ShowRequested += ShowWindow;
                _tray.ExitRequested += ExitApp;
            }
            catch { }
        }

        private void ShowWindow()
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                AppWindow.Show();
                NativeMethods.SetForegroundWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));
            });
        }

        private void ExitApp()
        {
            DispatcherQueue.TryEnqueue(delegate
            {
                SaveConfig();
                _tray?.Dispose();
                Application.Current.Exit();
            });
        }

        private void OnExitClick(object sender, RoutedEventArgs e)
        {
            SaveConfig();
            _tray?.Dispose();
            Application.Current.Exit();
        }

        private void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
        {
            args.Cancel = true;
            SaveConfig();
            AppWindow.Hide();
            if (!_trayHintShown)
            {
                _trayHintShown = true;
                Log(L.Get("TrayHint"));
            }
        }

        // ---------------- About ----------------

        private static string AppVersion
        {
            get
            {
                var v = typeof(App).Assembly.GetName().Version;
                return v == null ? "1.0.0" : v.ToString(3);
            }
        }

        private void OnRepoClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true });
        }

        private const string FirmwareUrl = "https://github.com/nonefffds/rotary-firmware";

        private void OnFirmwareClick(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(FirmwareUrl) { UseShellExecute = true });
        }

        private void OnLicensesClick(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.txt");
            if (!File.Exists(path))
                return;
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }

        private async void OnCheckUpdatesClick(object sender, RoutedEventArgs e)
        {
            UpdateResultText.Text = "";
            try
            {
                var uri = new Uri(RepoUrl);
                string[] parts = uri.AbsolutePath.Trim('/').Split('/');
                if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) ||
                    parts[0].Equals("YOUR-USER", StringComparison.OrdinalIgnoreCase))
                {
                    UpdateResultText.Text = L.Get("AboutNotConfigured");
                    return;
                }
                using (var client = new System.Net.Http.HttpClient())
                {
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("Rotary/" + AppVersion);
                    client.Timeout = TimeSpan.FromSeconds(10);
                    string api = "https://api.github.com/repos/" + parts[0] + "/" + parts[1] + "/releases/latest";
                    string json = await client.GetStringAsync(api);
                    string tag = ExtractTag(json);
                    if (string.IsNullOrEmpty(tag) || tag.TrimStart('v') == AppVersion)
                        UpdateResultText.Text = L.Get("UpToDate");
                    else
                        UpdateResultText.Text = string.Format(L.Get("UpdateAvailable"), tag);
                }
            }
            catch
            {
                UpdateResultText.Text = L.Get("UpdateFailed");
            }
        }

        private static string ExtractTag(string json)
        {
            int i = json.IndexOf("\"tag_name\":", StringComparison.Ordinal);
            if (i < 0) return "";
            i += "\"tag_name\":".Length;
            while (i < json.Length && (json[i] == ' ' || json[i] == '"')) i++;
            int j = i;
            while (j < json.Length && json[j] != '"') j++;
            return json.Substring(i, j - i);
        }
    }
}
