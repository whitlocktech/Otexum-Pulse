using System;
using System.Runtime.InteropServices;

namespace OtexumPulse.Util
{
    internal static class WinIdle
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO { public uint cbSize; public uint dwTime; }

        [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static TimeSpan GetIdleTime()
        {
            var lii = new LASTINPUTINFO { cbSize = (uint)Marshal.SizeOf<LASTINPUTINFO>() };
            if (!GetLastInputInfo(ref lii)) return TimeSpan.Zero;

            long nowMs = Environment.TickCount64;
            long delta = nowMs - lii.dwTime; // ms since last input
            if (delta < 0) delta = 0;
            return TimeSpan.FromMilliseconds(delta);
        }
    }
}
