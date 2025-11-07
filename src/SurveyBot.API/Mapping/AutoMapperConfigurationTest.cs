using AutoMapper;
using System.Reflection;

namespace SurveyBot.API.Mapping;

/// <summary>
/// Test helper to validate AutoMapper configuration.
/// This class can be used to ensure all mappings are correctly configured.
/// </summary>
public static class AutoMapperConfigurationTest
{
    /// <summary>
    /// Creates and validates the AutoMapper configuration.
    /// </summary>
    /// <returns>A configured IMapper instance.</returns>
    public static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg =>
        {
            // Add all profiles from the current assembly
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        // Validate configuration - throws if any issues found
        config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }

    /// <summary>
    /// Validates AutoMapper configuration without creating a mapper instance.
    /// Throws an exception if configuration is invalid.
    /// </summary>
    public static void ValidateConfiguration()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(Assembly.GetExecutingAssembly());
        });

        config.AssertConfigurationIsValid();
    }
}
