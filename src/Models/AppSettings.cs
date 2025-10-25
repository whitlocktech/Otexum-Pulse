namespace OtexumPulse.Models
{
    public class AppSettings
    {
        public string ExePath { get; set; } = "";
        public int IdleThresholdMinutes { get; set; } = 60;

        public bool StartWithWindows { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
    }
}
