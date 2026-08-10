using System.Text.Json;
using System.Text.Json.Nodes;
using FcaAssistant.App.Mapping;
using FcaAssistant.Extensions;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.App;

public class CarContext(IHaMqttClient haMqttClient, IVehicleDetailsMapper detailsMapper, Vehicle vehicle)
{
    public string Vin { get; } = vehicle.Vin;
    public HaDevice Device { get; } = new()
    {
        Name = string.IsNullOrEmpty(vehicle.Nickname) ? "Car" : vehicle.Nickname,
        Identifiers = [vehicle.Vin],
        Manufacturer = vehicle.Make,
        Model = vehicle.ModelDescription,
        Version = "1.0"
    };

    public HaSensor<HaLocation>? Location { get; private set;}
    public HaSensor? Timestamp { get; private set; }
    public Dictionary<string, HaSensor> Sensors { get; } = new();
    public Dictionary<string, HaEntity> Entities { get;} = new();

    public async Task ProcessLocationAsync(VehicleLocation location, string zone)
    {
        if (Location is null)
        {
            Location = new HaSensor<HaLocation>(Device, "CAR_LOCATION")
            {
                Icon = "mdi:map-marker"
            };
            await haMqttClient.AnnounceAsync(Location);
        }

        Location.State = zone;
        Location.Attributes = new HaLocation
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            SourceType = "gps",
            GpsAccuracy = 2
        };

        await haMqttClient.PublishAsync(Location);
    }

    public async Task ProcessDetailsAsync(JsonNode details, string targetUnit)
    {
        var properties = details.Flatten("car");
        foreach (var item in properties)
        {
            if (item.Key.EndsWith("_unit"))
                continue;

            var isExisting = Sensors.TryGetValue(item.Key, out var sensor);
            sensor ??= new HaSensor(Device, item.Key);
            
            var presentation = detailsMapper.Map(item.Key, item.Value, targetUnit, properties);
            sensor.State = presentation.State;
            sensor.DeviceClass = presentation.DeviceClass;
            sensor.UnitOfMeasurement = presentation.UnitOfMeasurement;

            if (!isExisting)
            {
                Sensors.Add(item.Key, sensor);
                await haMqttClient.AnnounceAsync(sensor);
            }

            await haMqttClient.PublishAsync(sensor);
        }
    }

    public async Task ProcessRemoteAsync(VehicleRemoteStatus remoteStatus)
    {
        var json = JsonSerializer.Serialize(remoteStatus);
        var status = JsonSerializer.Deserialize<JsonObject>(json);

        var flatten = status.Flatten("car_remote");
        foreach (var item in flatten)
        {
            if (!Sensors.TryGetValue(item.Key, out var sensor))
            {
                sensor = new HaSensor(Device, item.Key);
                Sensors.Add(item.Key, sensor);
                await haMqttClient.AnnounceAsync(sensor);
            }

            sensor.State = item.Value;

            await haMqttClient.PublishAsync(sensor);
        }
    }

    public async Task ProcessButtonAsync(string name, Func<HaButton, string, Task> action)
    {
        if (Entities.ContainsKey(name))
            return;

        var button = new HaButton(Device, name, action);
        Entities.Add(name, button);

        haMqttClient.Subscribe(button);
        await haMqttClient.AnnounceAsync(button);
    }

    public async Task ProcessSwitchAsync(string name, Func<HaSwitch, string, Task> action)
    {
        if (Entities.ContainsKey(name))
            return;

        var @switch = new HaSwitch(Device, name, action);
        Entities.Add(name, @switch);

        haMqttClient.Subscribe(@switch);
        await haMqttClient.AnnounceAsync(@switch);
    }

    public async Task ProcessTimestampAsync()
    {
        if (Timestamp is null)
        {
            Timestamp = new HaSensor(Device, "LAST_UPDATE")
            {
                DeviceClass = "timestamp",
                Icon = "mdi:timer-sync"
            };
            await haMqttClient.AnnounceAsync(Timestamp);
        }

        Timestamp.State = DateTime.Now.ToString("O");

        await haMqttClient.PublishAsync(Timestamp);
    }

    public async Task SetSwitchStateAsync(string name, bool isOn)
    {
        if (!Entities.TryGetValue(name, out var entity) || entity is not HaSwitch @switch)
            return;

        @switch.SetState(isOn);
        await haMqttClient.PublishAsync(@switch);
    }
}