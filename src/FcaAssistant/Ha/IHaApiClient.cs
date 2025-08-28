using FcaAssistant.Ha.Model;

namespace FcaAssistant.Ha;

public interface IHaApiClient
{
    Task<HaConfig> GetConfigAsync();
    Task<IReadOnlyList<HaRestApiEntityState>> GetStatesAsync();
}