using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;

namespace FcaAssistant.App.Handlers;

public class TimestampHandler(IHaEntityPublisher publisher) : IVehicleHandler
{
    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var sensor = new HaSensor(context.Device, "LAST_UPDATE")
        {
            DeviceClass = "timestamp",
            Icon = "mdi:timer-sync",
            State = DateTime.Now.ToString("O")
        };

        await publisher.PublishAsync(sensor);
    }
}
