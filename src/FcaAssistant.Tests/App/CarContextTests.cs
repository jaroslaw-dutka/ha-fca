using FcaAssistant.App;
using FcaAssistant.Tests.Fakes;

namespace FcaAssistant.Tests.App;

public class CarContextTests
{
    [Fact]
    public void Device_UsesNickname_WhenPresent()
    {
        var context = new CarContext(TestData.Vehicle(nickname: "Panda"), []);

        Assert.Equal("Panda", context.Device.Name);
    }

    [Fact]
    public void Device_FallsBackToCar_WhenNicknameEmpty()
    {
        var context = new CarContext(TestData.Vehicle(nickname: ""), []);

        Assert.Equal("Car", context.Device.Name);
    }

    [Fact]
    public void Device_MapsVehicleFields()
    {
        var context = new CarContext(TestData.Vehicle(vin: "VIN999"), []);

        Assert.Equal(["VIN999"], context.Device.Identifiers);
        Assert.Equal("Fiat", context.Device.Manufacturer);
        Assert.Equal("500e", context.Device.Model);
        Assert.Equal("1.0", context.Device.Version);
    }
}
