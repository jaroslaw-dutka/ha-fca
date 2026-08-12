using System.Globalization;
using FcaAssistant.App.Handlers;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Tests.Fakes;

namespace FcaAssistant.Tests.App.Handlers;

public class TimestampHandlerTests
{
    [Fact]
    public async Task PublishesLastUpdateTimestampSensor()
    {
        var publisher = new FakeHaEntityPublisher();
        var handler = new TimestampHandler(publisher);

        await handler.HandleAsync(TestData.Context(), CancellationToken.None);

        var sensor = Assert.IsType<HaSensor>(Assert.Single(publisher.Published));
        Assert.Equal("LAST_UPDATE", sensor.Name);
        Assert.Equal("timestamp", sensor.DeviceClass);
        Assert.True(DateTime.TryParse(sensor.State, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _));
    }
}
