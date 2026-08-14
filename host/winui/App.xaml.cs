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
            L.Init(Config.Language);
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            MainWindowRef = new MainWindow();
            MainWindowRef.Activate();
        }
    }
}
