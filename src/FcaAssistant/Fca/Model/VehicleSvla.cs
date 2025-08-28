using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class VehicleSvla
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("timestamp")]
    public int Timestamp { get; set; }
}