using System.Text.Json.Nodes;
using FcaAssistant.App;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.Tests.Fakes;

internal static class TestData
{
    public static VehicleInfo Vehicle(
        string vin = "VIN123",
        string nickname = "Car",
        JsonNode? details = null,
        JsonNode? remote = null,
        VehicleLocation? location = null) => new()
    {
        Vehicle = new Vehicle { Vin = vin, Nickname = nickname, Make = "Fiat", ModelDescription = "500e" },
        Location = location ?? new VehicleLocation(),
        Details = details ?? JsonNode.Parse("{}")!,
        Remote = remote ?? JsonNode.Parse("{}")!
    };

    public static CarContext Context(
        VehicleInfo? vehicle = null,
        IReadOnlyList<HaRestApiEntityState>? states = null) =>
        new(vehicle ?? Vehicle(), states ?? []);

    public static HaRestApiEntityState State(string entityId, string state, DateTimeOffset? lastChanged = null, string? friendlyName = null)
    {
        var attributes = new JsonObject();
        if (friendlyName is not null)
            attributes["friendly_name"] = friendlyName;

        return new HaRestApiEntityState
        {
            EntityId = entityId,
            State = state,
            LastChanged = lastChanged,
            Attributes = attributes
        };
    }

    public static HaRestApiEntityState Zone(string entityId, double latitude, double longitude, long radius, string friendlyName)
    {
        var attributes = new JsonObject
        {
            ["latitude"] = latitude,
            ["longitude"] = longitude,
            ["radius"] = radius,
            ["friendly_name"] = friendlyName
        };

        return new HaRestApiEntityState { EntityId = entityId, State = "zoning", Attributes = attributes };
    }
}
