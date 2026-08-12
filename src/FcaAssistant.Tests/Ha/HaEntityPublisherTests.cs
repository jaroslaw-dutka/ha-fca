using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Tests.Fakes;
using FcaAssistant.Tests.Ha.Entities;

namespace FcaAssistant.Tests.Ha;

public class HaEntityPublisherTests
{
    private readonly RecordingHaMqttClient _mqtt = new();
    private readonly HaEntityPublisher _publisher;

    public HaEntityPublisherTests() => _publisher = new HaEntityPublisher(_mqtt);

    private static HaSwitch Switch(string name = "Climate") =>
        new(TestDevice.Create(), name, (_, _) => Task.CompletedTask);

    private static HaSensor Sensor(string name = "car_odometer") => new(TestDevice.Create(), name);

    [Fact]
    public async Task RegisterAsync_AnnouncesAndSubscribes_WithoutPublishingState()
    {
        var @switch = Switch();

        await _publisher.RegisterAsync(@switch);

        Assert.Contains(@switch, _mqtt.Announced);
        Assert.Contains(@switch, _mqtt.Subscribed);
        Assert.Empty(_mqtt.Published);
    }

    [Fact]
    public async Task RegisterAsync_SameId_AnnouncesAndSubscribesOnce()
    {
        await _publisher.RegisterAsync(Switch());
        await _publisher.RegisterAsync(Switch());

        Assert.Single(_mqtt.Announced);
        Assert.Single(_mqtt.Subscribed);
    }

    [Fact]
    public async Task PublishAsync_FirstTime_AnnouncesThenPublishes()
    {
        var sensor = Sensor();

        await _publisher.PublishAsync(sensor);

        Assert.Contains(sensor, _mqtt.Announced);
        Assert.Contains(sensor, _mqtt.Published);
    }

    [Fact]
    public async Task PublishAsync_Repeated_AnnouncesOnceButPublishesEachTime()
    {
        await _publisher.PublishAsync(Sensor());
        await _publisher.PublishAsync(Sensor());

        Assert.Single(_mqtt.Announced);
        Assert.Equal(2, _mqtt.Published.Count);
    }

    [Fact]
    public async Task PublishAsync_AfterRegister_DoesNotReannounce()
    {
        var @switch = Switch();
        await _publisher.RegisterAsync(@switch);

        await _publisher.PublishAsync(@switch);

        Assert.Single(_mqtt.Announced);
        Assert.Single(_mqtt.Published);
    }

    [Fact]
    public async Task Get_ReturnsRegisteredEntity()
    {
        var @switch = Switch();
        await _publisher.RegisterAsync(@switch);

        Assert.Same(@switch, _publisher.Get<HaSwitch>(@switch.Id));
    }

    [Fact]
    public void Get_UnknownId_ReturnsNull()
    {
        Assert.Null(_publisher.Get<HaSwitch>("nope"));
    }

    [Fact]
    public async Task Get_WrongType_ReturnsNull()
    {
        var @switch = Switch();
        await _publisher.RegisterAsync(@switch);

        Assert.Null(_publisher.Get<HaButton>(@switch.Id));
    }
}
