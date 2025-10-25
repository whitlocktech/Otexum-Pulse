using Hardcodet.Wpf.TaskbarNotification;
using OtexumPulse.Models;
using OtexumPulse.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace OtexumPulse
{
    public partial class App : Application
    {
        private System.Threading.Mutex? _mutex;
        private TaskbarIcon? _tray;
        private IdleWatcher? _watcher;
        private AppSettings _settings = new();
        private Views.MainWindow? _window;

        protected override void OnStartup(StartupEventArgs e)
        {
            // single instance
            _mutex = new System.Threading.Mutex(false, @"Global\OtexumPulse_Singleton");
            if (!_mutex.WaitOne(0)) { Shutdown(); return; }

            _settings = SettingsService.Load();

            // tray icon (use app.ico if present; else no icon)
            BitmapImage? iconSource = null;
            try { iconSource = new BitmapImage(new Uri("pack://application:,,,/src/Resources/app.ico")); } catch { }

            _tray = new TaskbarIcon
            {
                IconSource = iconSource,
                ToolTipText = "Otexum Pulse"
            };

            // context menu
            var menu = new ContextMenu();
            var miOpen = new MenuItem { Header = "Open Settings" };
            var miPause = new MenuItem { Header = "Pause Watching" };
            var miExit = new MenuItem { Header = "Exit" };

            miOpen.Click += (_, __) => ShowSettings();
            miPause.Click += (_, __) => TogglePause(miPause);
            miExit.Click += (_, __) => ExitApp();

            menu.Items.Add(miOpen);
            menu.Items.Add(miPause);
            menu.Items.Add(new Separator());
            menu.Items.Add(miExit);

            _tray.ContextMenu = menu;
            _tray.TrayMouseDoubleClick += (_, __) => ShowSettings();

            // idle watcher
            _watcher = new IdleWatcher(_settings);
            _watcher.Start();

            if (!_settings.StartMinimized) ShowSettings();
            base.OnStartup(e);
            
            DispatcherUnhandledException += (_, args) =>
            {
                MessageBox.Show(args.Exception.ToString(), "Otexum Pulse – UI Error");
                args.Handled = true; // keep app alive
            };
            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                MessageBox.Show(args.ExceptionObject?.ToString() ?? "Unknown", "Otexum Pulse – Unhandled");
            };

        }

        private void TogglePause(MenuItem mi)
        {
            if (_watcher == null) return;
            if (_watcher.IsPaused) { _watcher.Resume(); mi.Header = "Pause Watching"; }
            else { _watcher.Pause(); mi.Header = "Resume Watching"; }
        }

        private void ShowSettings()
        {
            if (_window == null)
            {
                _window = new Views.MainWindow(_settings, OnSettingsSaved);
                _window.Closed += (_, __) => _window = null;
            }
            _window.Show();
            _window.Activate();
        }

        private void OnSettingsSaved(AppSettings s)
        {
            _settings = s;
            _watcher?.ApplySettings(s);
            if (_settings.StartWithWindows) StartupManager.Enable();
            else StartupManager.Disable();
        }

        private void ExitApp()
        {
            _tray?.Dispose();
            _watcher?.Dispose();
            _mutex?.ReleaseMutex();
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _tray?.Dispose();
            _watcher?.Dispose();
            _mutex?.Dispose();
            base.OnExit(e);
        }
    }
}
