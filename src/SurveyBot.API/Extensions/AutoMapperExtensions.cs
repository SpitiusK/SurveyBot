using System.Reflection;

namespace SurveyBot.API.Extensions;

/// <summary>
/// Extension methods for AutoMapper registration and configuration.
/// </summary>
public static class AutoMapperExtensions
{
    /// <summary>
    /// Registers AutoMapper with all mapping profiles from the API assembly.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services)
    {
        // Register AutoMapper with all profiles from the current assembly
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }

    /// <summary>
    /// Registers AutoMapper with mapping profiles from specified assemblies.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan for profiles.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAutoMapperProfiles(this IServiceCollection services, params Assembly[] assemblies)
    {
        // Register AutoMapper with profiles from specified assemblies
        services.AddAutoMapper(assemblies);

        return services;
    }
}
