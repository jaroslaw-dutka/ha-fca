using System.Text.Json.Nodes;
using FcaAssistant.Fca;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;

namespace FcaAssistant.Tests.Fakes;

/// <summary>Records the calls <see cref="FcaLiveClient"/> makes and returns canned responses.</summary>
public class FakeFcaApiClient : IFcaApiClient
{
    public int BootstrapCalls { get; private set; }
    public int AuthenticatePinCalls { get; private set; }
    public List<(string Vin, string Action, string Command)> SentCommands { get; } = [];

    public string PinToken { get; set; } = "pin-token";
    public Guid CommandCorrelationId { get; set; } = Guid.NewGuid();

    // The login handshake returns success by default so ConnectAsync establishes a session.
    public FcaBootstrapResponse BootstrapResponse { get; set; } = Ok(new FcaBootstrapResponse());
    public FcaLoginResponse LoginResponse { get; set; } =
        Ok(new FcaLoginResponse { UID = "uid-1", SessionInfo = new FcaSessionInfo { LoginToken = "login-token" } });
    public FcaTokenResponse TokenResponse { get; set; } = Ok(new FcaTokenResponse { IdToken = "id-token" });
    public FcaIdentityResponse IdentityResponse { get; set; } = new() { IdentityId = "identity-1", Token = "identity-token" };

    // Per-vehicle data returned by GetVehiclesAsync's aggregation.
    public List<Vehicle> Vehicles { get; } = [];
    public VehicleLocation Location { get; set; } = new();
    public JsonObject Details { get; set; } = new();
    public JsonObject Remote { get; set; } = new();

    private static T Ok<T>(T response) where T : FcaResponse
    {
        response.StatusCode = 200;
        return response;
    }

    public Task<FcaBootstrapResponse> Bootstrap()
    {
        BootstrapCalls++;
        return Task.FromResult(BootstrapResponse);
    }

    public Task<FcaLoginResponse> Login() => Task.FromResult(LoginResponse);
    public Task<FcaTokenResponse> GetToken(string loginToken) => Task.FromResult(TokenResponse);
    public Task<FcaIdentityResponse> GetIdentity(string idToken) => Task.FromResult(IdentityResponse);

    public Task<FcaPinAuthResponse> AuthenticatePin(FcaSession session, string pin)
    {
        AuthenticatePinCalls++;
        return Task.FromResult(new FcaPinAuthResponse { Token = PinToken });
    }

    public Task<FcaCommandResponse> SendCommand(FcaSession session, string pinToken, string vin, string action, string command)
    {
        SentCommands.Add((vin, action, command));
        return Task.FromResult(new FcaCommandResponse { CorrelationId = CommandCorrelationId });
    }

    public Task<VehicleResponse> GetVehicles(FcaSession session) => Task.FromResult(new VehicleResponse { Vehicles = Vehicles });
    public Task<VehicleLocation> GetVehicleLocation(FcaSession session, string vin) => Task.FromResult(Location);
    public Task<JsonObject> GetVehicleDetails(FcaSession session, string vin) => Task.FromResult(Details);
    public Task<JsonObject> GetVehicleRemoteStatus(FcaSession session, string vin) => Task.FromResult(Remote);
    public Task<NotificationsResponse> GetNotifications(FcaSession session) => Task.FromResult(new NotificationsResponse());
}
