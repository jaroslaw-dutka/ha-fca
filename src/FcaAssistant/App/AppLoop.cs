using FcaAssistant.Fca;
using FcaAssistant.Ha;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App;

/// <summary>
/// Owns the application lifecycle: initial delay, backend connections, and the polling loop that
/// drives <see cref="IVehicleProcessor"/> on every tick. Keeps looping across failures so a
/// transient backend error never stops the schedule.
/// </summary>
public class AppLoop(
    ILogger<AppLoop> logger,
    IOptions<AppSettings> appConfig,
    IOptions<FcaSettings> fcaConfig,
    IFcaClient fcaClient,
    IHaMqttClient haMqttClient,
    IRefreshTrigger refreshTrigger,
    IVehicleProcessor processor)
    : IAppService
{
    private readonly AppSettings _appSettings = appConfig.Value;
    private readonly FcaSettings _fcaSettings = fcaConfig.Value;

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
                await processor.ProcessAsync(cancellationToken);
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

            WaitHandle.WaitAny([cancellationToken.WaitHandle, refreshTrigger.Requested], TimeSpan.FromMinutes(_appSettings.RefreshInterval));
        }
    }
}
