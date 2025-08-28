namespace FiatChamp.App;

public record AppSettings
{
    public int RefreshInterval { get; set; }

    public string CarUnknownLocation { get; set; }

    public int StartDelaySeconds { get; set; }

    public bool AutoRefreshLocation { get; set; }

    public bool AutoRefreshBattery { get; set; }

    public bool EnableDangerousCommands { get; set; }

    public bool ConvertKmToMiles { get; set; }
}