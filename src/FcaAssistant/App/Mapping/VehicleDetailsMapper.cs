using System.Globalization;

namespace FcaAssistant.App.Mapping;

public class VehicleDetailsMapper : IVehicleDetailsMapper
{
    public SensorPresentation Map(string key, string value, string targetUnit, IReadOnlyDictionary<string, string> properties)
    {
        switch (key)
        {
            case "car_evInfo_battery_stateOfCharge":
                return new SensorPresentation(value, "battery", "%");
            case "car_evInfo_battery_timeToFullyChargeL2":
                return new SensorPresentation(value, "duration", "min");
        }

        // Only "_value" entries carry a companion "_unit" entry describing them.
        if (!key.EndsWith("_value"))
            return new SensorPresentation(value, null, null);

        properties.TryGetValue(key.Replace("_value", "_unit"), out var unit);

        var state = value;
        string? deviceClass = null;

        if (unit is "km" or "mi")
        {
            deviceClass = "distance";

            if (int.TryParse(value, out var rawValue))
            {
                var factor = $"{unit}->{targetUnit}" switch
                {
                    "km->mi" => 0.62137,
                    "mi->km" => 1.60934,
                    _ => 1.0
                };
                state = Math.Round(rawValue * factor, 2).ToString(CultureInfo.InvariantCulture);
                unit = targetUnit;
            }
        }

        return unit switch
        {
            "volts" => new SensorPresentation(state, "voltage", "V"),
            null or "null" => new SensorPresentation(state, deviceClass, ""),
            _ => new SensorPresentation(state, deviceClass, unit)
        };
    }
}
