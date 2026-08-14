using System;
using Microsoft.UI.Xaml;
using Windows.Globalization;

namespace RotaryMonitor
{
    public partial class App : Application
    {
        public static AppConfig Config = AppConfig.Load(AppConfig.DefaultPath);
        public static Window? MainWindowRef;

        public App()
        {
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
