using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;

namespace FcaAssistant.Fca;

public interface IFcaClient
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<List<VehicleInfo>> GetVehiclesAsync();
    Task<bool> TrySendCommandAsync(string vin, FcaCommand command);
}
