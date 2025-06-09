﻿using BlazorDatasheet.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorDatasheet.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBlazorDatasheet(this IServiceCollection services)
    {
        services.AddScoped<IMenuService, MenuService>();
        return services;
    }
}