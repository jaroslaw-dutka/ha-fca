using FcaAssistant.Fca.Entities;

namespace FcaAssistant.Fca;

public interface IFcaClient
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<List<VehicleInfo>> GetVehiclesAsync();
    Task SendCommandAsync(string vin, string command, string pin, string action);
    Task<bool> TrySendCommandAsync(string vin, string command, string pin, string action);
}