using Amazon;
using FcaAssistant.Fca;

namespace FcaAssistant.Tests.Fakes;

public class FakeFcaApiConfigProvider(FcaApiConfig? config = null) : IFcaApiConfigProvider
{
    private readonly FcaApiConfig _config = config ?? new FcaApiConfig
    {
        LoginUrl = "https://login.example.com",
        TokenUrl = "https://token.example.com",
        ApiUrl = "https://api.example.com",
        AuthUrl = "https://auth.example.com",
        LoginApiKey = "login-api-key",
        ApiKey = "api-key",
        AuthApiKey = "auth-api-key",
        Locale = "en_US",
        AwsEndpoint = RegionEndpoint.EUWest1
    };

    public FcaApiConfig Get() => _config;
}
