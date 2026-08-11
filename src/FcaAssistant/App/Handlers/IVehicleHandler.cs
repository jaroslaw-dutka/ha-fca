namespace FcaAssistant.App.Handlers;

public interface IVehicleHandler
{
    Task HandleAsync(CarContext context, CancellationToken cancellationToken);
}
