namespace FcaAssistant.App;

public record AppSettings
{
    public DistanceUnit DistanceUnit { get; set; }
    public int StartDelaySeconds { get; set; }
    public int RefreshInterval { get; set; }
    public bool AutoRefreshLocation { get; set; }
    public bool AutoRefreshBattery { get; set; }
    public bool EnableDangerousCommands { get; set; }
    public string? CarUnknownLocation { get; set; }
}