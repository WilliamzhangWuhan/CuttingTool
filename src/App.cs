using System;
using System.Windows;

namespace ScreenshotPin
{
    public sealed class App : Application
    {
        private TrayApplication _trayApplication;

        [STAThread]
        public static void Main()
        {
            var app = new App();
            app.Run();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _trayApplication = new TrayApplication(this);
            _trayApplication.Start();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_trayApplication != null)
            {
                _trayApplication.Dispose();
                _trayApplication = null;
            }

            base.OnExit(e);
        }
    }
}
