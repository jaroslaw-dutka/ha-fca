using System.Text.Json.Nodes;
using FcaAssistant.App.Mapping;
using FcaAssistant.Extensions;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.App;

public class CarContext(IHaEntityPublisher publisher, IVehicleDetailsMapper detailsMapper, Vehicle vehicle)
{
    public HaDevice Device { get; } = new()
    {
        Name = string.IsNullOrEmpty(vehicle.Nickname) ? "Car" : vehicle.Nickname,
        Identifiers = [vehicle.Vin],
        Manufacturer = vehicle.Make,
        Model = vehicle.ModelDescription,
        Version = "1.0"
    };

    public async Task ProcessLocationAsync(VehicleLocation location, string zone)
    {
        var sensor = new HaSensor<HaLocation>(Device, "CAR_LOCATION")
        {
            Icon = "mdi:map-marker",
            State = zone,
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

    public async Task ProcessSensorsAsync(JsonNode details, string targetUnit, string prefix)
    {
        var properties = details.Flatten(prefix);
        foreach (var item in properties.Where(item => !item.Key.EndsWith("_unit")))
        {
            var presentation = detailsMapper.Map(item.Key, item.Value, targetUnit, properties);
            var sensor = new HaSensor(Device, item.Key)
            {
                State = presentation.State,
                DeviceClass = presentation.DeviceClass,
                UnitOfMeasurement = presentation.UnitOfMeasurement
            };

            await publisher.PublishAsync(sensor);
        }
    }

    public async Task ProcessButtonAsync(string name, Func<HaButton, string, Task> action) =>
        await publisher.RegisterAsync(new HaButton(Device, name, action));

    public async Task ProcessSwitchAsync(string name, Func<HaSwitch, string, Task> action) =>
        await publisher.RegisterAsync(new HaSwitch(Device, name, action));

    public async Task ProcessTimestampAsync()
    {
        var sensor = new HaSensor(Device, "LAST_UPDATE")
        {
            DeviceClass = "timestamp",
            Icon = "mdi:timer-sync",
            State = DateTime.Now.ToString("O")
        };

        await publisher.PublishAsync(sensor);
    }

    public async Task SetSwitchStateAsync(string name, bool isOn)
    {
        if (publisher.Get<HaSwitch>(HaEntity.BuildId(Device, name)) is not { } @switch)
            return;

        @switch.SetState(isOn);
        await publisher.PublishAsync(@switch);
    }
}
