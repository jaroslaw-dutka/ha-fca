using FcaAssistant.Fca.Model;

namespace FcaAssistant.App;

public interface ICommandDispatcher
{
    WaitHandle RefreshRequested { get; }
    Task<bool> TrySendAsync(FcaCommand command, string vin);
    Task DispatchAsync(FcaCommand command, string vin);
}
