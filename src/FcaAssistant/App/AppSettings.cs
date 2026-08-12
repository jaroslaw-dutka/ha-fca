namespace FcaAssistant.App;

public record AppSettings
{
    public DistanceUnit DistanceUnit { get; set; }
    public int StartDelaySeconds { get; set; }
    public int RefreshInterval { get; set; }
    public bool AutoRefreshLocation { get; set; }
    public bool AutoRefreshBattery { get; set; }
    public string? CarUnknownLocation { get; set; }

    /// <summary>The HA unit distances are converted to, derived from <see cref="DistanceUnit"/>.</summary>
    public string TargetDistanceUnit => DistanceUnit == DistanceUnit.Miles ? "mi" : "km";
}