using Microsoft.Win32;
using System.Diagnostics;

namespace OtexumPulse.Services
{
    public static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName = "OtexumPulse";

        public static bool IsEnabled()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(RunKey, false);
            return rk?.GetValue(ValueName) is string;
        }

        public static void Enable()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(RunKey, true)
                         ?? Registry.CurrentUser.CreateSubKey(RunKey, true);
            var exe = Process.GetCurrentProcess().MainModule!.FileName;
            rk!.SetValue(ValueName, $"\"{exe}\"");
        }

        public static void Disable()
        {
            using var rk = Registry.CurrentUser.OpenSubKey(RunKey, true);
            rk?.DeleteValue(ValueName, false);
        }
    }
}
