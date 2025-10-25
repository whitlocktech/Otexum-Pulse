using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using OtexumPulse.Models;
using OtexumPulse.Util;

namespace OtexumPulse.Services
{
    public sealed class IdleWatcher : IDisposable
    {
        private const int CheckIntervalSeconds = 60;
        private CancellationTokenSource _cts = new();
        private Task? _loop;
        private AppSettings _s;

        public bool IsPaused { get; private set; }

        public IdleWatcher(AppSettings settings) => _s = settings;

        public void Start() => _loop = Task.Run(LoopAsync);
        public void Pause() => IsPaused = true;
        public void Resume() => IsPaused = false;
        public void ApplySettings(AppSettings s) => _s = s;

        public void Dispose()
        {
            _cts.Cancel();
            try { _loop?.Wait(500); } catch { }
            _cts.Dispose();
        }

        private static bool IsAppRunning(string exePath)
        {
            var name = Path.GetFileNameWithoutExtension(exePath) ?? "";
            if (string.IsNullOrWhiteSpace(name)) return false;

            // This avoids touching MainModule (no access denied spam)
            var running = Process.GetProcessesByName(name).Length > 0;
            foreach (var p in Process.GetProcessesByName(name)) p.Dispose();
            return running;
        }

        private async Task LoopAsync()
        {
            var ct = _cts.Token;
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    if (!IsPaused && File.Exists(_s.ExePath))
                    {
                        var idle = WinIdle.GetIdleTime();
                        if (idle.TotalMinutes >= _s.IdleThresholdMinutes && !IsAppRunning(_s.ExePath))
                        {
                            var psi = new ProcessStartInfo
                            {
                                FileName = _s.ExePath,
                                UseShellExecute = true,
                                WorkingDirectory = Path.GetDirectoryName(_s.ExePath)!
                            };
                            Process.Start(psi);
                        }
                    }
                }
                catch { /* keep looping */ }

                try { await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), ct); }
                catch { }
            }
        }
    }
}
