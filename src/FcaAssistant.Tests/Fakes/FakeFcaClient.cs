using FcaAssistant.Fca;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;

namespace FcaAssistant.Tests.Fakes;

public class FakeFcaClient : IFcaClient
{
    public List<(string Vin, FcaCommand Command)> Sent { get; } = [];
    public bool SendResult { get; set; } = true;

    public Task ConnectAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<List<VehicleInfo>> GetVehiclesAsync() => Task.FromResult(new List<VehicleInfo>());

    public Task<bool> TrySendCommandAsync(string vin, FcaCommand command)
    {
        Sent.Add((vin, command));
        return Task.FromResult(SendResult);
    }
}
