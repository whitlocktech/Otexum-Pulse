using Microsoft.Win32;
using OtexumPulse.Models;
using OtexumPulse.Services;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace OtexumPulse.Views
{
    public partial class MainWindow : Window
    {
        private AppSettings _settings;
        private readonly Action<AppSettings> _onSaved;

        // centralize bounds
        private const int IdleMin = 1;
        private const int IdleMax = 240;

        public MainWindow(AppSettings settings, Action<AppSettings> onSaved)
        {
            InitializeComponent();
            _settings = settings;
            _onSaved = onSaved;

            // bind current settings
            ExePathBox.Text = _settings.ExePath;
            IdleBox.Text = Math.Clamp(_settings.IdleThresholdMinutes, IdleMin, IdleMax).ToString();
            StartWithWindows.IsChecked = _settings.StartWithWindows;
            StartMinimized.IsChecked = _settings.StartMinimized;
        }

        // ---------- EXE picker ----------
        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Choose application to launch"
            };
            if (ofd.ShowDialog() is true)
                ExePathBox.Text = ofd.FileName;
        }

        // ---------- Idle minutes input (digits only + clamp) ----------
        private static readonly Regex _digits = new(@"^\d+$");
        private void IdleBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !_digits.IsMatch(e.Text);
        }

        private void IdleBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.SourceDataObject.GetDataPresent(DataFormats.Text)) { e.CancelCommand(); return; }
            var txt = e.SourceDataObject.GetData(DataFormats.Text) as string ?? "";
            if (!_digits.IsMatch(txt)) e.CancelCommand();
        }

        private void IdleBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter) { NormalizeIdleBox(); e.Handled = true; }
        }

        private void IdleBox_LostFocus(object sender, RoutedEventArgs e) => NormalizeIdleBox();

        private void NormalizeIdleBox()
        {
            if (!int.TryParse(IdleBox.Text, out var v)) v = _settings.IdleThresholdMinutes;
            v = Math.Clamp(v, IdleMin, IdleMax);
            IdleBox.Text = v.ToString();
        }

        // ---------- Save ----------
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(ExePathBox.Text))
            {
                MessageBox.Show(this, "Please choose a valid executable.", "Invalid path",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            NormalizeIdleBox(); // ensure clamped

            _settings.ExePath = ExePathBox.Text;
            _settings.IdleThresholdMinutes = int.Parse(IdleBox.Text);
            _settings.StartWithWindows = StartWithWindows.IsChecked == true;
            _settings.StartMinimized = StartMinimized.IsChecked == true;

            SettingsService.Save(_settings);
            _onSaved(_settings);

            MessageBox.Show(this, "Settings saved.", "Otexum Pulse",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Hide();
    }
}
