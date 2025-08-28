using System.Text.Json;
using Flurl.Http.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace FcaAssistant.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLogger(this IServiceCollection services, IConfiguration configuration)
    {
        var logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .CreateLogger();

        return services.AddLogging(i => i.AddSerilog(logger));
    }

    public static IServiceCollection AddHttp(this IServiceCollection services, IConfiguration configuration) => services
        .AddHttpClient()
        .AddSingleton<PollyRequestHandler>()
        .AddSingleton(sp => new FlurlClientCache().WithDefaults(builder =>
        {
            builder.Settings.JsonSerializer = new DefaultJsonSerializer(JsonSerializerOptions.Default);
            builder.AddMiddleware(sp.GetRequiredService<PollyRequestHandler>);
        }));
}