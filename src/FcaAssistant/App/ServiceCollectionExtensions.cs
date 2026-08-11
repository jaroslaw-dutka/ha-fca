using FcaAssistant.App.Handlers;
using FcaAssistant.App.Mapping;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FcaAssistant.App;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection services, IConfiguration configuration) => services
        .Configure<AppSettings>(configuration.GetSection("app"))
        .AddSingleton<IVehicleDetailsMapper, VehicleDetailsMapper>()
        .AddSingleton<ICommandDispatcher, CommandDispatcher>()
        .AddSingleton<IVehicleHandler, AutoRefreshHandler>()
        .AddSingleton<IVehicleHandler, LocationHandler>()
        .AddSingleton<IVehicleHandler, SensorsHandler>()
        .AddSingleton<IVehicleHandler, CommandEntitiesHandler>()
        .AddSingleton<IVehicleHandler, ClimateAutoOffHandler>()
        .AddSingleton<IVehicleHandler, TimestampHandler>()
        .AddSingleton<IAppService, AppService>();
}
