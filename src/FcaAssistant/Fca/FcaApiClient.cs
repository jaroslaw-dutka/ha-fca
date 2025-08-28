using System.Text;
using System.Text.Json.Nodes;
using FcaAssistant.Extensions;
using FcaAssistant.Fca.Entities;
using FcaAssistant.Fca.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Fca;

public class FcaApiClient : IFcaApiClient
{
    private readonly CookieJar _cookieJar = new();
    private readonly ILogger _logger;
    private readonly FcaSettings _settings;
    private readonly FcaApiConfig _apiConfig;
    private readonly IFlurlClient _flurlClient;

    public FcaApiClient(ILogger<FcaApiClient> logger, IOptions<FcaSettings> options, IFcaApiConfigProvider configProvider, IFlurlClientCache flurlClientCache)
    {
        _logger = logger;
        _settings = options.Value;
        _apiConfig = configProvider.Get();
        _flurlClient = flurlClientCache.GetOrAdd("fca_api");
    }
    
    public async Task<FcaBootstrapResponse> Bootstrap() => await _flurlClient
        .Request(_apiConfig.LoginUrl)
        .AppendPathSegment("accounts.webSdkBootstrap")
        .SetQueryParam("apiKey", _apiConfig.LoginApiKey)
        .WithCookies(_cookieJar)
        .GetJsonAsync<FcaBootstrapResponse>()
        .DumpResponseAsync(_logger);

    public async Task<FcaLoginResponse> Login() => await _flurlClient
        .Request(_apiConfig.LoginUrl)
        .AppendPathSegment("accounts.login")
        .WithCookies(_cookieJar)
        .PostUrlEncodedAsync(WithFcaParameters(new()
        {
            { "loginID", _settings.User },
            { "password", _settings.Password },
            { "sessionExpiration", TimeSpan.FromMinutes(5).TotalSeconds },
            { "include", "profile,data,emails,subscriptions,preferences" },
        }))
        .ReceiveJson<FcaLoginResponse>()
        .DumpResponseAsync(_logger);

    public async Task<FcaTokenResponse> GetToken(string loginToken) => await _flurlClient
        .Request(_apiConfig.LoginUrl)
        .AppendPathSegment("accounts.getJWT")
        .SetQueryParams(WithFcaParameters(new()
        {
            { "fields", "profile.firstName,profile.lastName,profile.email,country,locale,data.disclaimerCodeGSDP" },
            { "login_token", loginToken }
        }))
        .WithCookies(_cookieJar)
        .GetJsonAsync<FcaTokenResponse>()
        .DumpResponseAsync(_logger);

    public async Task<FcaIdentityResponse> GetIdentity(string idToken) => await _flurlClient
        .Request(_apiConfig.TokenUrl)
        .WithHeader("content-type", "application/json")
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .PostJsonAsync(new
        {
            gigya_token = idToken,
        })
        .ReceiveJson<FcaIdentityResponse>()
        .DumpResponseAsync(_logger);

    public async Task<FcaPinAuthResponse> AuthenticatePin(FcaSession session, string pin) => await _flurlClient
        .Request(_apiConfig.AuthUrl)
        .AppendPathSegments("v1", "accounts", session.UserId, "ignite", "pin", "authenticate")
        .WithHeaders(WithAwsHeaders(_apiConfig.AuthApiKey))
        .SignAwsAndPostJsonAsync(session.AwsCredentials, _apiConfig.AwsEndpoint, new
        {
            pin = Convert.ToBase64String(Encoding.UTF8.GetBytes(pin))
        })
        .ReceiveJson<FcaPinAuthResponse>()
        .DumpResponseAsync(_logger);

    public async Task<FcaCommandResponse> SendCommand(FcaSession session, string pinToken, string vin, string action, string command) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v1", "accounts", session.UserId, "vehicles", vin, action)
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAwsAndPostJsonAsync(session.AwsCredentials, _apiConfig.AwsEndpoint, new
        {
            command, pinAuth = pinToken
        })
        .ReceiveJson<FcaCommandResponse>()
        .DumpResponseAsync(_logger);

    public async Task<VehicleResponse> GetVehicles(FcaSession session) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v4", "accounts", session.UserId, "vehicles")
        .SetQueryParam("stage", "ALL")
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAws(session.AwsCredentials, _apiConfig.AwsEndpoint)
        .GetJsonAsync<VehicleResponse>()
        .DumpResponseAsync(_logger);

    public async Task<JsonObject> GetVehicleDetails(FcaSession session, string vin) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v2", "accounts", session.UserId, "vehicles", vin, "status")
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAws(session.AwsCredentials, _apiConfig.AwsEndpoint)
        .GetJsonAsync<JsonObject>()
        .DumpResponseAsync(_logger);

    public async Task<VehicleLocation> GetVehicleLocation(FcaSession session, string vin) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v1", "accounts", session.UserId, "vehicles", vin, "location", "lastknown")
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAws(session.AwsCredentials, _apiConfig.AwsEndpoint)
        .GetJsonAsync<VehicleLocation>()
        .DumpResponseAsync(_logger);

    public async Task<VehicleRemoteStatus> GetVehicleRemoteStatus(FcaSession session, string vin) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v1", "accounts", session.UserId, "vehicles", vin, "remote", "status")
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAws(session.AwsCredentials, _apiConfig.AwsEndpoint)
        .GetJsonAsync<VehicleRemoteStatus>()
        .DumpResponseAsync(_logger);

    public async Task<NotificationsResponse> GetNotifications(FcaSession session) => await _flurlClient
        .Request(_apiConfig.ApiUrl)
        .AppendPathSegments("v4", "accounts", session.UserId, "notifications", "summary")
        .SetQueryParam("brand", "ALL")
        .SetQueryParam("since", 1732399706408)
        .SetQueryParam("till", 1735647290831)
        .WithHeaders(WithAwsHeaders(_apiConfig.ApiKey))
        .SignAws(session.AwsCredentials, _apiConfig.AwsEndpoint)
        .GetJsonAsync<NotificationsResponse>()
        .DumpResponseAsync(_logger);

    private Dictionary<string, object> WithFcaParameters(Dictionary<string, object>? parameters = null)
    {
        var dict = new Dictionary<string, object>
        {
            { "targetEnv", "jssdk" },
            { "loginMode", "standard" },
            { "sdk", "js_latest" },
            { "authMode", "cookie" },
            { "sdkBuild", "12234" },
            { "format", "json" },
            { "APIKey", _apiConfig.LoginApiKey },
        };

        foreach (var parameter in parameters ?? new())
            dict.Add(parameter.Key, parameter.Value);

        return dict;
    }

    private Dictionary<string, object> WithAwsHeaders(string apiKey) => new()
    {
        { "x-api-key", apiKey },
        { "x-clientapp-name", "CWP" },
        { "x-clientapp-version", "1.0" },
        { "x-originator-type", "web" },
        { "clientrequestid", _apiConfig.ClientId },
        { "locale", _apiConfig.Locale }
    };
}