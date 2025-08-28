namespace FcaAssistant.App;

public interface IAppService
{
    Task RunAsync(CancellationToken cancellationToken);
}