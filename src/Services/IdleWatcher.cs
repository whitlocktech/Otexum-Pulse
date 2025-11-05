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
        private bool _hasTriggered = false;
        private static readonly TimeSpan ActivityWindow = TimeSpan.FromSeconds(1);
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
                    if (!IsPaused)
                    {
                        var exe = _s.ExePath;
                        if (!string.IsNullOrWhiteSpace(exe) && File.Exists(exe))
                        {
                            var idle = WinIdle.GetIdleTime();

                            // Any user input → reset the once-per-idle trigger latch
                            if (idle < ActivityWindow)
                                _hasTriggered = false;

                            var threshold = TimeSpan.FromMinutes(Math.Max(1, _s.IdleThresholdMinutes));

                            // Fire once per idle stretch, and don’t relaunch if already running
                            if (!_hasTriggered && idle >= threshold && !IsAppRunning(exe))
                            {
                                try
                                {
                                    var wd = Path.GetDirectoryName(exe) ?? Environment.CurrentDirectory;
                                    var psi = new ProcessStartInfo
                                    {
                                        FileName = exe,
                                        UseShellExecute = true,
                                        WorkingDirectory = wd
                                    };
                                    Process.Start(psi);
                                    _hasTriggered = true; // latch until user activity
                                }
                                catch { /* swallow and keep the app alive */ }
                            }
                        }
                    }
                }
                catch { /* never let the loop die */ }

                try { await Task.Delay(TimeSpan.FromSeconds(CheckIntervalSeconds), ct); }
                catch { /* cancellation or transient error */ }
            }
        }
    }
}
