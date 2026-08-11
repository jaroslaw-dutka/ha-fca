using FcaAssistant.Fca;
using FcaAssistant.Fca.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.App;

public class CommandDispatcher(
    ILogger<CommandDispatcher> logger,
    IOptions<AppSettings> appConfig,
    IOptions<FcaSettings> fcaConfig,
    IFcaClient fcaClient)
    : ICommandDispatcher
{
    private readonly AutoResetEvent _refreshRequested = new(false);
    private readonly AppSettings _appSettings = appConfig.Value;
    private readonly FcaSettings _fcaSettings = fcaConfig.Value;

    public WaitHandle RefreshRequested => _refreshRequested;

    public async Task<bool> TrySendAsync(FcaCommand command, string vin)
    {
        if (command.IsDangerous && !_appSettings.EnableDangerousCommands)
        {
            logger.LogWarning("{command} not sent. Set \"EnableDangerousCommands\" option if you want to use it. ", command.Message);
            return false;
        }

        return await fcaClient.TrySendCommandAsync(vin, command.Message, _fcaSettings.Pin, command.Action);
    }

    public async Task DispatchAsync(FcaCommand command, string vin)
    {
        if (await TrySendAsync(command, vin))
            _refreshRequested.Set();
    }
}
