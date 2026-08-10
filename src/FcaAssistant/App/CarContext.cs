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
    public HaDevice Device { get; } = new()
    {
        Name = string.IsNullOrEmpty(vehicle.Nickname) ? "Car" : vehicle.Nickname,
        Identifiers = [vehicle.Vin],
        Manufacturer = vehicle.Make,
        Model = vehicle.ModelDescription,
        Version = "1.0"
    };

    private HaSensor<HaLocation>? _location;
    private HaSensor? _timestamp;
    private readonly Dictionary<string, HaSensor> _sensors = new();
    private readonly Dictionary<string, HaEntity> _entities = new();

    public async Task ProcessLocationAsync(VehicleLocation location, string zone)
    {
        if (_location is null)
        {
            _location = new HaSensor<HaLocation>(Device, "CAR_LOCATION")
            {
                Icon = "mdi:map-marker"
            };
            await haMqttClient.AnnounceAsync(_location);
        }

        _location.State = zone;
        _location.Attributes = new HaLocation
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            SourceType = "gps",
            GpsAccuracy = 2
        };

        await haMqttClient.PublishAsync(_location);
    }

    public async Task ProcessSensorsAsync(JsonNode details, string targetUnit, string prefix)
    {
        var properties = details.Flatten(prefix);
        foreach (var item in properties.Where(item => !item.Key.EndsWith("_unit"))) 
            await PublishSensorAsync(item.Key, detailsMapper.Map(item.Key, item.Value, targetUnit, properties));
    }

    public async Task ProcessButtonAsync(string name, Func<HaButton, string, Task> action)
    {
        if (_entities.ContainsKey(name))
            return;

        var button = new HaButton(Device, name, action);
        _entities.Add(name, button);

        haMqttClient.Subscribe(button);
        await haMqttClient.AnnounceAsync(button);
    }

    public async Task ProcessSwitchAsync(string name, Func<HaSwitch, string, Task> action)
    {
        if (_entities.ContainsKey(name))
            return;

        var @switch = new HaSwitch(Device, name, action);
        _entities.Add(name, @switch);

        haMqttClient.Subscribe(@switch);
        await haMqttClient.AnnounceAsync(@switch);
    }

    public async Task ProcessTimestampAsync()
    {
        if (_timestamp is null)
        {
            _timestamp = new HaSensor(Device, "LAST_UPDATE")
            {
                DeviceClass = "timestamp",
                Icon = "mdi:timer-sync"
            };
            await haMqttClient.AnnounceAsync(_timestamp);
        }

        _timestamp.State = DateTime.Now.ToString("O");

        await haMqttClient.PublishAsync(_timestamp);
    }

    public async Task SetSwitchStateAsync(string name, bool isOn)
    {
        if (!_entities.TryGetValue(name, out var entity) || entity is not HaSwitch @switch)
            return;

        @switch.SetState(isOn);
        await haMqttClient.PublishAsync(@switch);
    }

    private async Task PublishSensorAsync(string key, SensorPresentation presentation)
    {
        var isNew = !_sensors.TryGetValue(key, out var sensor);
        sensor ??= new HaSensor(Device, key);

        sensor.State = presentation.State;
        sensor.DeviceClass = presentation.DeviceClass;
        sensor.UnitOfMeasurement = presentation.UnitOfMeasurement;

        if (isNew)
        {
            _sensors.Add(key, sensor);
            await haMqttClient.AnnounceAsync(sensor);
        }

        await haMqttClient.PublishAsync(sensor);
    }
}