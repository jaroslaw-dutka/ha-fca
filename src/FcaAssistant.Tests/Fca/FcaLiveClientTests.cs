using FcaAssistant.Fca;
using FcaAssistant.Fca.Model;
using FcaAssistant.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.Fca;

public class FcaLiveClientTests
{
    private readonly FakeFcaApiClient _api = new();
    private readonly FakeCognitoIdentityClient _cognito = new();

    private FcaLiveClient CreateClient(bool enableDangerousCommands = false, string? pin = "1234") => new(
        NullLogger<FcaLiveClient>.Instance,
        Options.Create(new FcaSettings { Pin = pin, EnableDangerousCommands = enableDangerousCommands }),
        new FakeFcaApiConfigProvider(),
        _api,
        _cognito,
        new FakeMqttClientFactory(new FakeMqttClient()));

    // A cancelled token lets ConnectAsync run the (network-free) login handshake while the background
    // MQTT connect loop bails out immediately instead of dialling the real AWS IoT endpoint.
    private static CancellationToken Cancelled()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        return cts.Token;
    }

    [Fact]
    public async Task TrySendCommandAsync_DangerousCommandWhenDisabled_IsRejectedWithoutTouchingApi()
    {
        var result = await CreateClient(enableDangerousCommands: false)
            .TrySendCommandAsync("VIN", FcaCommands.DoorsLock);

        Assert.False(result);
        Assert.Equal(0, _api.AuthenticatePinCalls);
        Assert.Empty(_api.SentCommands);
    }

    [Fact]
    public async Task TrySendCommandAsync_WithoutSession_SwallowsErrorAndReturnsFalse()
    {
        // A safe command clears the dangerous guard, but no session has been established yet.
        var result = await CreateClient().TrySendCommandAsync("VIN", FcaCommands.Blink);

        Assert.False(result);
        Assert.Empty(_api.SentCommands);
    }

    [Fact]
    public async Task GetVehiclesAsync_WithoutSession_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => CreateClient().GetVehiclesAsync());
    }

    [Fact]
    public async Task ConnectAsync_RunsLoginHandshakeAndEstablishesUsableSession()
    {
        _api.Vehicles.Add(new Vehicle { Vin = "VIN-1" });
        var client = CreateClient();

        await client.ConnectAsync(Cancelled());

        // A session now exists, so the previously-guarded vehicle retrieval succeeds and aggregates
        // the per-vehicle location/details/remote slices.
        var vehicle = Assert.Single(await client.GetVehiclesAsync());
        Assert.Equal("VIN-1", vehicle.Vehicle.Vin);
        Assert.Same(_api.Location, vehicle.Location);
        Assert.Same(_api.Details, vehicle.Details);
        Assert.Same(_api.Remote, vehicle.Remote);
    }

    [Fact]
    public async Task ConnectAsync_WhenAlreadyConnected_DoesNotLogInAgain()
    {
        var client = CreateClient();

        await client.ConnectAsync(Cancelled());
        await client.ConnectAsync(Cancelled());

        Assert.Equal(1, _api.BootstrapCalls);
    }

    [Fact]
    public async Task ConnectAsync_WhenBootstrapReportsError_Throws()
    {
        _api.BootstrapResponse = new FcaBootstrapResponse { StatusCode = 401 };

        await Assert.ThrowsAnyAsync<Exception>(() => CreateClient().ConnectAsync(Cancelled()));
    }
}
