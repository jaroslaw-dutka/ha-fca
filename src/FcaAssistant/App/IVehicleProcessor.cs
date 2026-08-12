namespace FcaAssistant.App;

public interface IVehicleProcessor
{
    Task ProcessAsync(CancellationToken cancellationToken);
}
