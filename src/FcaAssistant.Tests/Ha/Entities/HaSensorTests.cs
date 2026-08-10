using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.Tests.Ha.Entities;

public class HaSensorTests
{
    [Fact]
    public void Metadata_IsDerivedFromDeviceAndName()
    {
        var sensor = new HaSensor(TestDevice.Create(), "car_odometer");

        Assert.Equal("sensor", sensor.Type);
        Assert.Equal("car_odometer", sensor.Name);
        Assert.Equal("VIN123_car_odometer", sensor.Id);
    }

    [Fact]
    public void DefaultIcon_IsEye()
    {
        var sensor = new HaSensor(TestDevice.Create(), "x");

        Assert.Equal("mdi:eye", sensor.Icon);
    }

    [Fact]
    public async Task OnSetAsync_WithoutAction_DoesNotThrow()
    {
        var sensor = new HaSensor(TestDevice.Create(), "x");

        await sensor.OnSetAsync("value");
    }

    [Fact]
    public async Task OnSetAsync_WithAction_InvokesIt()
    {
        string? received = null;
        var sensor = new HaSensor(TestDevice.Create(), "x", (_, state) =>
        {
            received = state;
            return Task.CompletedTask;
        });

        await sensor.OnSetAsync("value");

        Assert.Equal("value", received);
    }

    [Fact]
    public void GenericSensor_SerializesAttributesAsJson()
    {
        var sensor = new HaSensor<HaLocation>(TestDevice.Create(), "CAR_LOCATION")
        {
            Attributes = new HaLocation
            {
                Latitude = 50.5,
                Longitude = 19.25,
                SourceType = "gps",
                GpsAccuracy = 2
            }
        };

        Assert.Equal(
            """{"latitude":50.5,"longitude":19.25,"source_type":"gps","gps_accuracy":2}""",
            sensor.SerializedAttributes);
    }
}
