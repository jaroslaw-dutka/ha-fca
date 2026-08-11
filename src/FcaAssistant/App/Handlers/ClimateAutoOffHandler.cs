using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App.Handlers;

public class ClimateAutoOffHandler(
    IHaEntityPublisher publisher,
    IOptions<AppSettings> appConfig,
    ILogger<ClimateAutoOffHandler> logger)
    : IVehicleHandler
{
    private const string SwitchName = "Climate";

    private readonly AppSettings _appSettings = appConfig.Value;

    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var entityState = FindSwitchState(context.States, context.Device.Name, SwitchName);
        if (entityState is null)
        {
            logger.LogDebug("Auto-off: Climate switch not found in HA states for device {Device}.", context.Device.Name);
            return;
        }

        if (!string.Equals(entityState.State, "on", StringComparison.OrdinalIgnoreCase) || entityState.LastChanged is null)
            return;

        var onFor = DateTimeOffset.UtcNow - entityState.LastChanged.Value;
        if (onFor < TimeSpan.FromMinutes(_appSettings.RefreshInterval))
            return;

        if (publisher.Get<HaSwitch>(HaEntity.BuildId(context.Device, SwitchName)) is not { } @switch)
            return;

        logger.LogInformation("Auto-clearing Climate switch in HomeAssistant after {Minutes:F0} min ON (no OFF command sent).", onFor.TotalMinutes);
        @switch.SetState(false);
        await publisher.PublishAsync(@switch);
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
}
