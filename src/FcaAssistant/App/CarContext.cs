using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using FcaAssistant.Extensions;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.App;

public class CarContext(IHaMqttClient haMqttClient, Vehicle vehicle)
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
        var flatten = details.Flatten("car");
        foreach (var item in flatten)
        {
            if (item.Key.EndsWith("_unit"))
                continue;

            if (!Sensors.TryGetValue(item.Key, out var sensor))
            {
                sensor = new HaSensor(Device, item.Key);
                Sensors.Add(item.Key, sensor);
                await haMqttClient.AnnounceAsync(sensor);
            }

            sensor.State = item.Value;

            if (sensor.Name == "car_evInfo_battery_stateOfCharge")
            {
                sensor.DeviceClass = "battery";
                sensor.UnitOfMeasurement = "%";
            }
            else if (sensor.Name == "car_evInfo_battery_timeToFullyChargeL2")
            {
                sensor.DeviceClass = "duration";
                sensor.UnitOfMeasurement = "min";
            }
            else if (item.Key.EndsWith("_value"))
            {
                var unitKey = item.Key.Replace("_value", "_unit");

                flatten.TryGetValue(unitKey, out var tmpUnit);

                if (tmpUnit is "km" or "mi")
                {
                    sensor.DeviceClass = "distance";

                    // TODO: verify 
                    if (int.TryParse(item.Value, out var rawValue))
                    {
                        var factor = $"{tmpUnit}->{targetUnit}" switch
                        {
                            "km->mi" => 0.62137,
                            "mi->km" => 1.60934,
                            _ => 1.0
                        };
                        sensor.State = Math.Round(rawValue * factor, 2).ToString(CultureInfo.InvariantCulture);
                        tmpUnit = targetUnit;
                    }
                }

                switch (tmpUnit)
                {
                    case "volts":
                        sensor.DeviceClass = "voltage";
                        sensor.UnitOfMeasurement = "V";
                        break;
                    case null or "null":
                        sensor.UnitOfMeasurement = "";
                        break;
                    default:
                        sensor.UnitOfMeasurement = tmpUnit;
                        break;
                }
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