using FcaAssistant.Fca;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App.Handlers;

public class AutoRefreshHandler(IFcaClient fcaClient, IOptions<AppSettings> appConfig) : IVehicleHandler
{
    private readonly AppSettings _appSettings = appConfig.Value;

    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var vin = context.Vehicle.Vehicle.Vin;

        if (_appSettings.AutoRefreshBattery)
            await fcaClient.TrySendCommandAsync(vin, FcaCommands.DeepRefresh);

        if (_appSettings.AutoRefreshLocation)
            await fcaClient.TrySendCommandAsync(vin, FcaCommands.VehicleFinder);
    }
}
 