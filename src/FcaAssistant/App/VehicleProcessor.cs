using FcaAssistant.App.Handlers;
using FcaAssistant.Extensions;
using FcaAssistant.Fca;
using FcaAssistant.Ha;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App;

/// <summary>
/// Runs a single processing pass: pulls the current Home Assistant state, then feeds every vehicle
/// through the handler pipeline. Scheduling and error resilience are the caller's concern (see
/// <see cref="AppLoop"/>).
/// </summary>
public class VehicleProcessor(
    ILogger<VehicleProcessor> logger,
    IOptions<AppSettings> appConfig,
    IFcaClient fcaClient,
    IHaApiClient haApiClient,
    IEnumerable<IVehicleHandler> handlers)
    : IVehicleProcessor
{
    private readonly AppSettings _appSettings = appConfig.Value;
    private readonly IReadOnlyList<IVehicleHandler> _handlers = handlers.ToList();

    public async Task ProcessAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching new data...");

        var config = await haApiClient.GetConfigAsync();
        logger.LogInformation("Using unit system: {unit}", config.UnitSystem.Dump());
        logger.LogInformation("Distance conversion: {sourceUnit}->{targetUnit}", config.UnitSystem.Length, _appSettings.TargetDistanceUnit);

        var states = await haApiClient.GetStatesAsync();

        foreach (var vehicleInfo in await fcaClient.GetVehiclesAsync())
        {
            logger.LogInformation("Processing CAR: {vin}", vehicleInfo.Vehicle.Vin);

            var context = new CarContext(vehicleInfo, states);
            foreach (var handler in _handlers)
                await handler.HandleAsync(context, cancellationToken);
        }

        logger.LogInformation("Processing COMPLETED.");
    }
}
