using FcaAssistant.Fca;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App.Handlers;

public class AutoRefreshHandler(ICommandDispatcher dispatcher, IOptions<AppSettings> appConfig) : IVehicleHandler
{
    private readonly AppSettings _appSettings = appConfig.Value;

    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var vin = context.Vehicle.Vehicle.Vin;

        if (_appSettings.AutoRefreshBattery)
            await dispatcher.TrySendAsync(FcaCommands.DeepRefresh, vin);

        if (_appSettings.AutoRefreshLocation)
            await dispatcher.TrySendAsync(FcaCommands.VehicleFinder, vin);
    }
}
 