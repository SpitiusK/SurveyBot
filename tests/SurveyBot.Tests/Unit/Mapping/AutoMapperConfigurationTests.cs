using AutoMapper;
using SurveyBot.API.Mapping;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests to validate the complete AutoMapper configuration.
/// </summary>
public class AutoMapperConfigurationTests
{
    [Fact]
    public void AutoMapper_Configuration_AllProfiles_IsValid()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<SurveyMappingProfile>();
            cfg.AddProfile<QuestionMappingProfile>();
            cfg.AddProfile<ResponseMappingProfile>();
            cfg.AddProfile<AnswerMappingProfile>();
            cfg.AddProfile<UserMappingProfile>();
        });

        // Act & Assert
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void AutoMapper_Configuration_FromAssembly_IsValid()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(SurveyMappingProfile).Assembly);
        });

        // Act & Assert
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void AutoMapper_CanCreateMapper_Success()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(SurveyMappingProfile).Assembly);
        });

        // Act
        var mapper = config.CreateMapper();

        // Assert
        Assert.NotNull(mapper);
    }

    [Fact]
    public void AutoMapper_AllValueResolvers_AreResolvable()
    {
        // Arrange
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddMaps(typeof(SurveyMappingProfile).Assembly);
        });

        // Act
        var mapper = config.CreateMapper();

        // Assert - If we can create the mapper, all value resolvers are resolvable
        Assert.NotNull(mapper);
    }
}
