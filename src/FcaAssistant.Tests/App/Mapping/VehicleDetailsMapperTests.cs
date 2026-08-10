using FcaAssistant.App.Mapping;

namespace FcaAssistant.Tests.App.Mapping;

public class VehicleDetailsMapperTests
{
    private readonly VehicleDetailsMapper _mapper = new();

    private SensorPresentation Map(string key, string value, string targetUnit = "km", params (string Key, string Value)[] properties)
    {
        var dict = properties.ToDictionary(p => p.Key, p => p.Value);
        // The mapped key itself is normally part of the flattened set; include it for realism.
        dict[key] = value;
        return _mapper.Map(key, value, targetUnit, dict);
    }

    [Fact]
    public void BatteryStateOfCharge_IsBatteryPercent()
    {
        var result = Map("car_evInfo_battery_stateOfCharge", "80");

        Assert.Equal(new SensorPresentation("80", "battery", "%"), result);
    }

    [Fact]
    public void BatteryTimeToFullyCharge_IsDurationMinutes()
    {
        var result = Map("car_evInfo_battery_timeToFullyChargeL2", "45");

        Assert.Equal(new SensorPresentation("45", "duration", "min"), result);
    }

    [Fact]
    public void NonValueKey_HasNoDeviceClassOrUnit()
    {
        var result = Map("car_evInfo_battery_plugStatus", "true");

        Assert.Equal(new SensorPresentation("true", null, null), result);
    }

    [Fact]
    public void ValueKey_WithVoltsUnit_IsVoltage()
    {
        var result = Map("car_battery_value", "12", properties: ("car_battery_unit", "volts"));

        Assert.Equal(new SensorPresentation("12", "voltage", "V"), result);
    }

    [Fact]
    public void ValueKey_WithoutCompanionUnit_HasEmptyUnitAndNoDeviceClass()
    {
        var result = Map("car_something_value", "7");

        Assert.Equal(new SensorPresentation("7", null, ""), result);
    }

    [Fact]
    public void ValueKey_WithLiteralNullUnit_HasEmptyUnitAndNoDeviceClass()
    {
        var result = Map("car_something_value", "7", properties: ("car_something_unit", "null"));

        Assert.Equal(new SensorPresentation("7", null, ""), result);
    }

    [Fact]
    public void ValueKey_WithUnknownUnit_KeepsUnitAndNoDeviceClass()
    {
        var result = Map("car_tirePressure_value", "32", properties: ("car_tirePressure_unit", "psi"));

        Assert.Equal(new SensorPresentation("32", null, "psi"), result);
    }

    [Fact]
    public void Distance_SameUnit_IsUnchangedButClassifiedAsDistance()
    {
        var result = Map("car_odometer_value", "100", "km", ("car_odometer_unit", "km"));

        Assert.Equal(new SensorPresentation("100", "distance", "km"), result);
    }

    [Fact]
    public void Distance_KmToMiles_IsConvertedAndRounded()
    {
        var result = Map("car_range_value", "100", "mi", ("car_range_unit", "km"));

        Assert.Equal(new SensorPresentation("62.14", "distance", "mi"), result);
    }

    [Fact]
    public void Distance_MilesToKm_IsConvertedAndRounded()
    {
        var result = Map("car_range_value", "100", "km", ("car_range_unit", "mi"));

        Assert.Equal(new SensorPresentation("160.93", "distance", "km"), result);
    }

    [Fact]
    public void Distance_NonNumericValue_IsClassifiedButNotConverted()
    {
        var result = Map("car_range_value", "n/a", "mi", ("car_range_unit", "km"));

        // Parsing fails, so the original value and source unit are kept.
        Assert.Equal(new SensorPresentation("n/a", "distance", "km"), result);
    }

    [Fact]
    public void Distance_DecimalValue_IsNotConverted()
    {
        // Only integers are parsed for conversion; a decimal string falls through unchanged.
        var result = Map("car_range_value", "100.5", "mi", ("car_range_unit", "km"));

        Assert.Equal(new SensorPresentation("100.5", "distance", "km"), result);
    }

    [Fact]
    public void Distance_MilesToMiles_IsUnchanged()
    {
        var result = Map("car_range_value", "50", "mi", ("car_range_unit", "mi"));

        Assert.Equal(new SensorPresentation("50", "distance", "mi"), result);
    }
}
