using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class VehicleTC
{
    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}