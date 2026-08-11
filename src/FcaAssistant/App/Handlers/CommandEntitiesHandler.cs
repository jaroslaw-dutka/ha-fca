using FcaAssistant.Fca;
using FcaAssistant.Fca.Model;
using FcaAssistant.Ha;
using FcaAssistant.Ha.Entities;
using FcaAssistant.Ha.Model;
using Microsoft.Extensions.Logging;

namespace FcaAssistant.App.Handlers;

public class CommandEntitiesHandler(
    IHaEntityPublisher publisher,
    ICommandDispatcher dispatcher,
    ILogger<CommandEntitiesHandler> logger)
    : IVehicleHandler
{
    public async Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        var device = context.Device;
        var vin = context.Vehicle.Vehicle.Vin;

        await BindButton(device, vin, "Blink", FcaCommands.Blink);
        await BindButton(device, vin, "Charge", FcaCommands.ChargeNow);
        await BindButton(device, vin, "UpdateAll", FcaCommands.DeepRefresh);
        await BindButton(device, vin, "UpdateLocation", FcaCommands.VehicleFinder);
        await BindButton(device, vin, "UpdateBattery", FcaCommands.DeepRefresh);

        await BindSwitch(device, vin, "Doors", FcaCommands.DoorsUnlock, FcaCommands.DoorsLock);
        await BindSwitch(device, vin, "Climate", FcaCommands.ClimateOn, FcaCommands.ClimateOff);
        await BindSwitch(device, vin, "Trunk", FcaCommands.TrunkUnlock, FcaCommands.TrunkLock);
    }

    private Task BindButton(HaDevice device, string vin, string name, FcaCommand command) =>
        publisher.RegisterAsync(new HaButton(device, name, async (_, state) =>
        {
            logger.LogDebug("Button {Name} clicked to state: {State}", name, state);
            await dispatcher.DispatchAsync(command, vin);
        }));

    private Task BindSwitch(HaDevice device, string vin, string name, FcaCommand onCommand, FcaCommand offCommand) =>
        publisher.RegisterAsync(new HaSwitch(device, name, async (entity, state) =>
        {
            logger.LogDebug("Switch {Name} changed to state: {State}", name, state);
            await dispatcher.DispatchAsync(entity.IsOn ? onCommand : offCommand, vin);
        }));
}
