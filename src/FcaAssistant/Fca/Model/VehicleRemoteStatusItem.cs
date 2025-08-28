using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class VehicleRemoteStatusItem
{
    [JsonPropertyName("status")]
    public string Status { get; set; }
}