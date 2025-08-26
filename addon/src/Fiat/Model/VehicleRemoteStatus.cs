using System.Text.Json.Serialization;

namespace FiatChamp.Fiat.Model;

public class VehicleRemoteStatus
{
    [JsonPropertyName("doors")]
    public Dictionary<string, VehicleRemoteStatusItem> Doors { get; set; }

    [JsonPropertyName("evRunning")]
    public VehicleRemoteStatusItem EvRunning { get; set; }

    [JsonPropertyName("trunk")]
    public VehicleRemoteStatusItem Trunk { get; set; }

    [JsonPropertyName("windows")]
    public Dictionary<string, VehicleRemoteStatusItem> Windows { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}