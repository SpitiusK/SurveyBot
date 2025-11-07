using AutoMapper;
using SurveyBot.API.Mapping;
using SurveyBot.Core.DTOs.User;
using SurveyBot.Core.Entities;
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
        var user = new User
        {
            Id = 1,
            TelegramId = 123456789,
            Username = "testuser",
            FirstName = "John",
            LastName = "Doe",
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow
        };

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
        var user = new User
        {
            Id = 2,
            TelegramId = 987654321,
            Username = null,
            FirstName = "Jane",
            LastName = null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

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
            Username = "newuser",
            FirstName = "New",
            LastName = "User"
        };

        // Act
        var user = _mapper.Map<User>(loginDto);

        // Assert
        Assert.NotNull(user);
        Assert.Equal(loginDto.TelegramId, user.TelegramId);
        Assert.Equal(loginDto.Username, user.Username);
        Assert.Equal(loginDto.FirstName, user.FirstName);
        Assert.Equal(loginDto.LastName, user.LastName);
    }
}
