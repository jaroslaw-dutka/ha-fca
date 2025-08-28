﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FiatChamp.Ha;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddHa(this IServiceCollection services, IConfiguration configuration) => services
        .Configure<HaApiSettings>(configuration.GetSection("ha:api"))
        .Configure<HaMqttSettings>(configuration.GetSection("ha:mqtt"))
        .AddSingleton<IHaApiClient, HaApiClient>()
        .AddSingleton<IHaMqttClient, HaMqttClient>();
}