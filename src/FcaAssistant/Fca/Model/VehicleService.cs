using System.Text.Json.Serialization;

namespace FcaAssistant.Fca.Model;

public class VehicleService
{
    [JsonPropertyName("vehicleCapable")]
    public bool VehicleCapable { get; set; }

    [JsonPropertyName("service")]
    public string Service { get; set; }

    [JsonPropertyName("serviceEnabled")]
    public bool ServiceEnabled { get; set; }
}