using FcaAssistant.Ha;
using FcaAssistant.Ha.Model;

namespace FcaAssistant.Tests.Fakes;

public class FakeHaApiClient : IHaApiClient
{
    public HaConfig Config { get; set; } = new() { UnitSystem = new HaUnitSystem { Length = "km" } };
    public List<HaRestApiEntityState> States { get; } = [];

    public Task<HaConfig> GetConfigAsync() => Task.FromResult(Config);

    public Task<IReadOnlyList<HaRestApiEntityState>> GetStatesAsync() =>
        Task.FromResult<IReadOnlyList<HaRestApiEntityState>>(States);
}
