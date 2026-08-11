using System.Text.Json.Nodes;
using FcaAssistant.App.Mapping;
using FcaAssistant.Extensions;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App.Handlers;

public class SensorsHandler(IHaEntityPublisher publisher, IVehicleDetailsMapper detailsMapper, IOptions<AppSettings> appConfig) : IVehicleHandler
{
    private readonly string _targetUnit = appConfig.Value.TargetDistanceUnit;

    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        await PublishSensorsAsync(context, context.Vehicle.Details, "car");
        await PublishSensorsAsync(context, context.Vehicle.Remote, "car_remote");
    }

    private async Task PublishSensorsAsync(CarContext context, JsonNode data, string prefix)
    {
        var properties = data.Flatten(prefix);
        foreach (var item in properties.Where(item => !item.Key.EndsWith("_unit")))
        {
            var presentation = detailsMapper.Map(item.Key, item.Value, _targetUnit, properties);
            var sensor = new HaSensor(context.Device, item.Key)
            {
                State = presentation.State,
                DeviceClass = presentation.DeviceClass,
                UnitOfMeasurement = presentation.UnitOfMeasurement
            };

            await publisher.PublishAsync(sensor);
        }
    }
}
