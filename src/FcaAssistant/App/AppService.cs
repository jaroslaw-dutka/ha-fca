using System.Collections.Concurrent;
using System.Text.Json;
using CoordinateSharp;
using FcaAssistant.Extensions;
using FcaAssistant.Fca;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Model;
using Flurl.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App;

public class AppService : IAppService
{
    private readonly AutoResetEvent _forceLoopResetEvent = new(false);
    private readonly ConcurrentDictionary<string, CarContext> _cars = new();

    private readonly ILogger<AppService> _logger;
    private readonly AppSettings _appSettings;
    private readonly FcaSettings _fcaSettings;
    private readonly IFcaClient _fcaClient;
    private readonly IHaApiClient _haApiClient;
    private readonly IHaMqttClient _haMqttClient;

    public AppService(ILogger<AppService> logger, IOptions<AppSettings> appConfig, IOptions<FcaSettings> fcaConfig, IFcaClient fcaClient, IHaApiClient haApiClient, IHaMqttClient haMqttClient)
    {
        _logger = logger;
        _appSettings = appConfig.Value;
        _fcaSettings = fcaConfig.Value;
        _fcaClient = fcaClient;
        _haApiClient = haApiClient;
        _haMqttClient = haMqttClient;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Delay start for seconds: {delay}", _appSettings.StartDelaySeconds);
        await Task.Delay(TimeSpan.FromSeconds(_appSettings.StartDelaySeconds), cancellationToken);

        _logger.LogInformation("Connecting to HomeAssistant");
        await _haMqttClient.ConnectAsync(cancellationToken);

        _logger.LogInformation("Connecting to {brand}", _fcaSettings.Brand);
        await _fcaClient.ConnectAsync(cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await FetchData(cancellationToken);
                _logger.LogInformation("Processing COMPLETED.");
            }
            catch (FlurlHttpException exception)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    var responseTask = exception.Call?.Response?.GetStringAsync();
                    var response = responseTask != null ? await responseTask : string.Empty;
                    _logger.LogDebug(exception, "Processing FAILED. STATUS: {status}, MESSAGE: {message}, RESPONSE: {response}", exception.StatusCode, exception.Message, response);
                }
                else
                    _logger.LogWarning("Processing FAILED. Error connecting to the {brand} API. This can happen from time to time.", _fcaSettings.Brand);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Processing FAILED");
            }
            finally
            {
                _logger.LogInformation("Next update in {delay} minutes.", _appSettings.RefreshInterval);
            }
                
            WaitHandle.WaitAny([cancellationToken.WaitHandle, _forceLoopResetEvent], TimeSpan.FromMinutes(_appSettings.RefreshInterval));
        }
    }

    private async Task FetchData(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching new data...");

        var config = await _haApiClient.GetConfigAsync();
        _logger.LogInformation("Using unit system: {unit}", config.UnitSystem.Dump());

        var targetUnit = _appSettings.DistanceUnit == DistanceUnit.Miles ? "mi" : "km";
        _logger.LogInformation("Distance conversion: {sourceUnit}->{targetUnit}", config.UnitSystem.Length, targetUnit);

        var states = await _haApiClient.GetStatesAsync();
        var zones = states
            .Where(state => state.EntityId.StartsWith("zone."))
            .Select(state => state.Attributes.Deserialize<HaRestApiZone>()!)
            .ToList();

        foreach (var vehicleInfo in await _fcaClient.GetVehiclesAsync())
        {
            _logger.LogInformation("Processing CAR: {vin}", vehicleInfo.Vehicle.Vin);

            if (_appSettings.AutoRefreshBattery) 
                await TrySendCommand(FcaCommands.DeepRefresh, vehicleInfo.Vehicle.Vin);

            if (_appSettings.AutoRefreshLocation) 
                await TrySendCommand(FcaCommands.VehicleFinder, vehicleInfo.Vehicle.Vin);

            if (!_cars.TryGetValue(vehicleInfo.Vehicle.Vin, out var context))
            {
                context = new CarContext(_haMqttClient, vehicleInfo.Vehicle);
                _cars.TryAdd(vehicleInfo.Vehicle.Vin, context);
            }

            //var enabledServices = vehicleInfo.Vehicle.Services.Where(i => i is { VehicleCapable: true, ServiceEnabled: true }).Select(i => i.Service).ToList();

            // Location
            var currentZone = GetZone(zones, vehicleInfo.Location);
            await context.ProcessLocationAsync(vehicleInfo.Location, currentZone);

            // Details
            await context.ProcessDetailsAsync(vehicleInfo.Details, targetUnit);

            // Remote
            await context.ProcessRemoteAsync(vehicleInfo.Remote);

            // Buttons
            await BindButton(context, "Blink", FcaCommands.Blink, vehicleInfo.Vehicle.Vin);
            await BindButton(context, "Charge", FcaCommands.ChargeNow, vehicleInfo.Vehicle.Vin);
            await BindButton(context, "UpdateAll", FcaCommands.DeepRefresh, vehicleInfo.Vehicle.Vin);
            await BindButton(context, "UpdateLocation", FcaCommands.VehicleFinder, vehicleInfo.Vehicle.Vin);
            await BindButton(context, "UpdateBattery", FcaCommands.DeepRefresh, vehicleInfo.Vehicle.Vin);

            // Switches
            await BindSwitch(context, "Doors", FcaCommands.DoorsUnlock, FcaCommands.DoorsLock, vehicleInfo.Vehicle.Vin);
            await BindSwitch(context, "Climate", FcaCommands.ClimateOn, FcaCommands.ClimateOff, vehicleInfo.Vehicle.Vin);
            await BindSwitch(context, "Trunk", FcaCommands.TrunkUnlock, FcaCommands.TrunkLock, vehicleInfo.Vehicle.Vin);

            await AutoOffClimateAsync(context, states);

            // Timestamp
            await context.ProcessTimestampAsync();
        }
    }

    private string GetZone(IReadOnlyList<HaRestApiZone> zones, VehicleLocation location)
    {
        var coordinate = new Coordinate(location.Latitude, location.Longitude);
        var zone = zones
            .Select(zone => new { Zone = zone, DistanceToZone = new Coordinate(zone.Latitude, zone.Longitude).Get_Distance_From_Coordinate(coordinate).Meters })
            .Where(item => item.DistanceToZone <= item.Zone.Radius)
            .OrderBy(item => item.DistanceToZone)
            .Select(i => i.Zone)
            .FirstOrDefault();
        return zone?.FriendlyName ?? _appSettings.CarUnknownLocation;
    }

    private async Task BindButton(CarContext context, string name, FcaCommand command, string vin) => await context.ProcessButtonAsync(name, async (entity, state) =>
    {
        _logger.LogDebug("Button {Name} clicked to state: {State}", name, state);
        if (await TrySendCommand(command, vin))
            _forceLoopResetEvent.Set();
    });

    private async Task BindSwitch(CarContext context, string name, FcaCommand onCommand, FcaCommand offCommand, string vin) => await context.ProcessSwitchAsync(name, async (entity, state) =>
    {
        _logger.LogDebug("Switch {Name} changed to state: {State}", name, state);
        if (await TrySendCommand(entity.IsOn ? onCommand : offCommand, vin))
            _forceLoopResetEvent.Set();
    });

    private async Task AutoOffClimateAsync(CarContext context, IReadOnlyList<HaRestApiEntityState> states)
    {
        var entityState = FindSwitchState(states, context.Device.Name, "Climate");
        if (entityState is null)
        {
            _logger.LogDebug("Auto-off: Climate switch not found in HA states for device {Device}.", context.Device.Name);
            return;
        }

        if (!string.Equals(entityState.State, "on", StringComparison.OrdinalIgnoreCase) || entityState.LastChanged is null)
            return;

        var onFor = DateTimeOffset.UtcNow - entityState.LastChanged.Value;
        if (onFor < TimeSpan.FromMinutes(_appSettings.RefreshInterval))
            return;

        _logger.LogInformation("Auto-clearing Climate switch in HomeAssistant after {Minutes:F0} min ON (no OFF command sent).", onFor.TotalMinutes);
        await context.SetSwitchStateAsync("Climate", false);
    }

    // HA derives the entity_id / friendly_name from the device and entity names, so match on either.
    private static HaRestApiEntityState? FindSwitchState(IReadOnlyList<HaRestApiEntityState> states, string deviceName, string switchName)
    {
        var expectedEntityId = $"switch.{Slugify(deviceName)}_{Slugify(switchName)}";
        var expectedFriendlyName = $"{deviceName} {switchName}";

        return states.FirstOrDefault(state =>
            string.Equals(state.EntityId, expectedEntityId, StringComparison.OrdinalIgnoreCase) ||
            (state.EntityId.StartsWith("switch.", StringComparison.OrdinalIgnoreCase) &&
             state.Attributes.TryGetPropertyValue("friendly_name", out var friendlyName) &&
             string.Equals(friendlyName?.GetValue<string>(), expectedFriendlyName, StringComparison.OrdinalIgnoreCase)));
    }

    // Approximates Home Assistant's slugify: lower-case, non-alphanumerics collapsed to single underscores, trimmed.
    private static string Slugify(string value)
    {
        var slug = new string(value.ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());
        while (slug.Contains("__"))
            slug = slug.Replace("__", "_");
        return slug.Trim('_');
    }

    private async Task<bool> TrySendCommand(FcaCommand command, string vin)
    {
        if (command.IsDangerous && !_appSettings.EnableDangerousCommands)
        {
            _logger.LogWarning("{command} not sent. Set \"EnableDangerousCommands\" option if you want to use it. ", command.Message);
            return false;
        }

        return await _fcaClient.TrySendCommandAsync(vin, command.Message, _fcaSettings.Pin, command.Action);
    }
}