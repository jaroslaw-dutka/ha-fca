using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class VehicleResponse
{
    [JsonPropertyName("userid")]
    public string Userid { get; set; }

    [JsonPropertyName("version")]
    public long Version { get; set; }

    [JsonPropertyName("vehicles")]
    public List<Vehicle> Vehicles { get; set; }
}