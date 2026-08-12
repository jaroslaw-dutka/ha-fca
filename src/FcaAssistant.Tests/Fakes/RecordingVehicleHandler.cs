using FcaAssistant.App;
using FcaAssistant.App.Handlers;

namespace FcaAssistant.Tests.Fakes;

/// <summary>
/// Captures every <see cref="CarContext"/> it is handed, optionally invoking a callback (used to
/// assert relative ordering across handlers or to make a handler throw).
/// </summary>
public class RecordingVehicleHandler(Action<CarContext>? onHandle = null) : IVehicleHandler
{
    public List<CarContext> Handled { get; } = [];

    public Task HandleAsync(CarContext context, CancellationToken cancellationToken)
    {
        Handled.Add(context);
        onHandle?.Invoke(context);
        return Task.CompletedTask;
    }
}
