using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.User;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using Xunit;

namespace SurveyBot.Tests.Unit.Mapping;

/// <summary>
/// Tests for User entity AutoMapper mappings.
/// </summary>
public class UserMappingTests
{
    private readonly IMapper _mapper;

    public UserMappingTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void AutoMapper_Configuration_IsValid()
    {
        // Arrange & Act & Assert
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<UserMappingProfile>();
        });
        config.AssertConfigurationIsValid();
    }

    [Fact]
    public void Map_User_To_UserDto_Success()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(
            telegramId: 123456789,
            username: "testuser",
            firstName: "John",
            lastName: "Doe"
        );
        user.SetId(1);
        user.SetCreatedAt(DateTime.UtcNow.AddDays(-30));
        user.SetUpdatedAt(DateTime.UtcNow);

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.TelegramId, dto.TelegramId);
        Assert.Equal(user.Username, dto.Username);
        Assert.Equal(user.FirstName, dto.FirstName);
        Assert.Equal(user.LastName, dto.LastName);
        Assert.Equal(user.CreatedAt, dto.CreatedAt);
        Assert.Equal(user.UpdatedAt, dto.UpdatedAt);
    }

    [Fact]
    public void Map_User_With_Nullable_Fields_Success()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(
            telegramId: 987654321,
            username: null,
            firstName: "Jane",
            lastName: null
        );
        user.SetId(2);
        user.SetCreatedAt(DateTime.UtcNow);
        user.SetUpdatedAt(DateTime.UtcNow);

        // Act
        var dto = _mapper.Map<UserDto>(user);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.TelegramId, dto.TelegramId);
        Assert.Null(dto.Username);
        Assert.Equal("Jane", dto.FirstName);
        Assert.Null(dto.LastName);
    }

    [Fact]
    public void Map_LoginDto_To_User_Success()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            TelegramId = 111222333,
            Username = "newuser"
        };

        // Act
        var user = _mapper.Map<User>(loginDto);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(loginDto.TelegramId, user.TelegramId);
        Assert.Equal(loginDto.Username, user.Username);
        // FirstName and LastName are not part of LoginDto mapping
        // They are populated separately during authentication flow
    }
}
