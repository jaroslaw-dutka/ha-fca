using System.Text.Json;
using CoordinateSharp;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App.Handlers;

public class LocationHandler(IHaEntityPublisher publisher, IOptions<AppSettings> appConfig) : IVehicleHandler
{
    private readonly AppSettings _appSettings = appConfig.Value;

    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var location = context.Vehicle.Location;

        var sensor = new HaSensor<HaLocation>(context.Device, "CAR_LOCATION")
        {
            Icon = "mdi:map-marker",
            State = GetZone(context.States, location),
            Attributes = new HaLocation
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                SourceType = "gps",
                GpsAccuracy = 2
            }
        };

        await publisher.PublishAsync(sensor);
    }

    private string GetZone(IReadOnlyList<HaRestApiEntityState> states, VehicleLocation location)
    {
        var zones = states
            .Where(state => state.EntityId.StartsWith("zone."))
            .Select(state => state.Attributes.Deserialize<HaRestApiZone>()!);

        var coordinate = new Coordinate(location.Latitude, location.Longitude);
        var zone = zones
            .Select(zone => new { Zone = zone, DistanceToZone = new Coordinate(zone.Latitude, zone.Longitude).Get_Distance_From_Coordinate(coordinate).Meters })
            .Where(item => item.DistanceToZone <= item.Zone.Radius)
            .OrderBy(item => item.DistanceToZone)
            .Select(i => i.Zone)
            .FirstOrDefault();
        return zone?.FriendlyName ?? _appSettings.CarUnknownLocation;
    }
}
