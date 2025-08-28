using System.Text.Json.Nodes;
using FcaAssistant.Fca.Model;

namespace FcaAssistant.Fca.Entities;

public class VehicleInfo
{
    public Vehicle Vehicle { get; set; }
    public JsonNode Details { get; set; }
    public VehicleLocation Location { get; set; }
    public VehicleRemoteStatus Remote { get; set; }
}