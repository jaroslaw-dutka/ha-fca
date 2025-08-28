using FcaAssistant.Fca.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Fca;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFca(this IServiceCollection services, IConfiguration configuration) => services
        .Configure<FcaSettings>(configuration.GetSection("fca"))
        .AddSingleton<IFcaApiConfigProvider, FcaApiConfigProvider>()
        .AddSingleton<IFcaApiClient, FcaApiClient>()
        .AddSingleton<FcaLiveClient>()
        .AddSingleton<FcaMockClient>()
        .AddSingleton<IFcaClient>(s => s.GetRequiredService<IOptions<FcaSettings>>().Value.Brand switch
        {
            FcaBrand.Mock => s.GetRequiredService<FcaMockClient>(),
            _ => s.GetRequiredService<FcaLiveClient>(),
        });
}