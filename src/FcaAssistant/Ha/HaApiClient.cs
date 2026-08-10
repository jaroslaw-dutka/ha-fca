using FcaAssistant.Ha.Model;
using Flurl.Http;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Ha;

public class HaApiClient(IOptions<HaApiSettings> options, IFlurlClientCache flurlClientCache)
    : IHaApiClient
{
    private readonly HaApiSettings _settings = options.Value;
    private readonly IFlurlClient _flurlClient = flurlClientCache.GetOrAdd("ha_api");

    public async Task<HaConfig> GetConfigAsync() => await _flurlClient
        .Request(_settings.Url)
        .AppendPathSegment("config")
        .WithOAuthBearerToken(_settings.Token)
        .GetJsonAsync<HaConfig>();

    public async Task<IReadOnlyList<HaRestApiEntityState>> GetStatesAsync() => await _flurlClient
        .Request(_settings.Url)
        .AppendPathSegment("states")
        .WithOAuthBearerToken(_settings.Token)
        .GetJsonAsync<HaRestApiEntityState[]>();
}