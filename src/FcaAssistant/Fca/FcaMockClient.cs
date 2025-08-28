using System.Text.Json;
using System.Text.Json.Nodes;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;

namespace FcaAssistant.Fca;

public class FcaMockClient : IFcaClient
{
    private readonly ILogger<FcaMockClient> _logger;
    private readonly DefaultJsonSerializer _serializer = new(JsonSerializerOptions.Default);

    public FcaMockClient(ILogger<FcaMockClient> logger)
    {
        _logger = logger;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken) => 
        await Task.Delay(1000, cancellationToken);

    public Task<List<VehicleInfo>> GetVehiclesAsync() => Task.FromResult(new List<VehicleInfo>
    {
        new()
        {
            Vehicle = _serializer.Deserialize<Vehicle>(File.OpenRead("./Mocks/vehicles.json")),
            Location = _serializer.Deserialize<VehicleLocation>(File.OpenRead("./Mocks/location.json")),
            Details = _serializer.Deserialize<JsonObject>(File.OpenRead("./Mocks/details.json"))
        }
    });

    public async Task SendCommandAsync(string vin, string command, string pin, string action)
    {
        _logger.LogInformation("Mock sending command {Command} with action {Action} to vehicle {Vin}", command, action, vin);
        await Task.Delay(1000);
    }

    public async Task<bool> TrySendCommandAsync(string vin, string command, string pin, string action)
    {
        _logger.LogInformation("Mock try sending command {Command} with action {Action} to vehicle {Vin}", command, action, vin);
        await Task.Delay(1000);
        return true;
    }
}