using FiatChamp.Fiat.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FiatChamp.Fiat;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFiat(this IServiceCollection services, IConfiguration configuration) => services
        .Configure<FiatSettings>(configuration.GetSection("fiat"))
        .AddSingleton<IFiatApiConfigProvider, FiatApiConfigProvider>()
        .AddSingleton<IFiatApiClient, FiatApiClient>()
        .AddSingleton<FiatLiveClient>()
        .AddSingleton<FiatMockClient>()
        .AddSingleton<IFiatClient>(s => s.GetRequiredService<IOptions<FiatSettings>>().Value.Backend switch
        {
            FcaBackend.Live => s.GetRequiredService<FiatLiveClient>(),
            FcaBackend.Mock => s.GetRequiredService<FiatMockClient>(),
            _ => throw new ArgumentOutOfRangeException()
        });
}