using System.Text.Json.Nodes;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;

namespace FcaAssistant.Fca;

public interface IFcaApiClient
{
    Task<FcaBootstrapResponse> Bootstrap();
    Task<FcaLoginResponse> Login();
    Task<FcaTokenResponse> GetToken(string loginToken);
    Task<FcaIdentityResponse> GetIdentity(string idToken);
    Task<FcaPinAuthResponse> AuthenticatePin(FcaSession session, string pin);
    Task<FcaCommandResponse> SendCommand(FcaSession session, string pinToken, string vin, string action, string command);
    Task<VehicleResponse> GetVehicles(FcaSession session);
    Task<JsonObject> GetVehicleDetails(FcaSession session, string vin);
    Task<VehicleLocation> GetVehicleLocation(FcaSession session, string vin);
    Task<VehicleRemoteStatus> GetVehicleRemoteStatus(FcaSession session, string vin);
    Task<NotificationsResponse> GetNotifications(FcaSession session);
}