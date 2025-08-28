using System.Text.Json.Serialization;

namespace FiatChamp.Fiat.Model;

public class VehicleRemoteStatusItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}