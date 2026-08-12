using FcaAssistant.App.Handlers;
using FcaAssistant.Extensions;
using FcaAssistant.Fca;
using FcaAssistant.Ha;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App;

public class AppService(
    ILogger<AppService> logger,
    IOptions<AppSettings> appConfig,
    IOptions<FcaSettings> fcaConfig,
    IFcaClient fcaClient,
    IHaApiClient haApiClient,
    IHaMqttClient haMqttClient,
    IEnumerable<IVehicleHandler> handlers)
    : IAppService, IRefreshTrigger
{
    private readonly AppSettings _appSettings = appConfig.Value;
    private readonly FcaSettings _fcaSettings = fcaConfig.Value;
    private readonly IReadOnlyList<IVehicleHandler> _handlers = handlers.ToList();
    private readonly AutoResetEvent _refreshRequested = new(false);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Delay start for seconds: {delay}", _appSettings.StartDelaySeconds);
        await Task.Delay(TimeSpan.FromSeconds(_appSettings.StartDelaySeconds), cancellationToken);

        logger.LogInformation("Connecting to HomeAssistant");
        await haMqttClient.ConnectAsync(cancellationToken);

        logger.LogInformation("Connecting to {brand}", _fcaSettings.Brand);
        await fcaClient.ConnectAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
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
            catch (FlurlHttpException exception)
            {
                if (logger.IsEnabled(LogLevel.Debug))
                {
                    var responseTask = exception.Call?.Response?.GetStringAsync();
                    var response = responseTask != null ? await responseTask : string.Empty;
                    logger.LogDebug(exception, "Processing FAILED. STATUS: {status}, MESSAGE: {message}, RESPONSE: {response}", exception.StatusCode, exception.Message, response);
                }
                else
                    logger.LogWarning("Processing FAILED. Error connecting to the {brand} API. This can happen from time to time.", _fcaSettings.Brand);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Processing FAILED");
            }
            finally
            {
                logger.LogInformation("Next update in {delay} minutes.", _appSettings.RefreshInterval);
            }

            WaitHandle.WaitAny([cancellationToken.WaitHandle, _refreshRequested], TimeSpan.FromMinutes(_appSettings.RefreshInterval));
        }
    }

    public void Trigger() =>
        _refreshRequested.Set();
}
