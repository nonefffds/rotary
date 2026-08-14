using System;
using System.Threading;
using Microsoft.UI.Xaml;
using Windows.Globalization;

namespace RotaryMonitor
{
    public partial class App : Application
    {
        public static AppConfig Config = AppConfig.Load(AppConfig.DefaultPath);
        public static Window? MainWindowRef;

        private const string MutexName = "Local\\RotaryMonitor_8C1F6C2A0F2B4F5D";
        private const string ShowEventName = "Local\\RotaryMonitorShow_8C1F6C2A0F2B4F5D";
        private static Mutex? _mutex;
        private static EventWaitHandle? _showEvent;

        public App()
        {
            // Single instance: if another instance is already running, ask it to
            // show its window and exit this one.
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (!createdNew)
            {
                try
                {
                    using (var ev = EventWaitHandle.OpenExisting(ShowEventName))
                        ev.Set();
                }
                catch { }
                Environment.Exit(0);
                return;
            }
            _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
            StartShowListener();

            // Localization is driven by Config.Language through L.Init; the
            // ApplicationLanguages override is avoided because it crashes in
            // unpackaged/self-contained WinUI 3 builds.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogCrash(e.ExceptionObject as Exception);
            try
            {
                L.Init(Config.Language);
                InitializeComponent();
            }
            catch (Exception ex)
            {
                LogCrash(ex);
                throw;
            }
        }

        private void StartShowListener()
        {
            var t = new Thread(delegate ()
            {
                while (true)
                {
                    try { _showEvent?.WaitOne(); } catch { break; }
                    if (MainWindowRef != null)
                    {
                        MainWindowRef.DispatcherQueue.TryEnqueue(delegate
                        {
                            try
                            {
                                MainWindowRef.AppWindow.Show();
                                NativeMethods.SetForegroundWindow(
                                    WinRT.Interop.WindowNative.GetWindowHandle(MainWindowRef));
                            }
                            catch { }
                        });
                    }
                }
            }) { IsBackground = true };
            t.Start();
        }

        private static void LogCrash(Exception? ex)
        {
            try
            {
                System.IO.File.WriteAllText(
                    System.IO.Path.Combine(AppContext.BaseDirectory, "rotary_crash.log"),
                    ex == null ? "unknown exception" : ex.ToString());
            }
            catch { }
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindowRef = new MainWindow();
            MainWindowRef.Activate();
        }
    }
}
