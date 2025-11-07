using FluentAssertions;
using SurveyBot.Core.Entities;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Helpers;

namespace SurveyBot.Tests.Unit.Repositories;

public class GenericRepositoryTests : RepositoryTestBase
{
    private readonly GenericRepository<User> _repository;

    public GenericRepositoryTests()
    {
        _repository = new GenericRepository<User>(_context);
    }

    [Fact]
    public async Task CreateAsync_ValidEntity_ReturnsEntityWithId()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();

        // Act
        var result = await _repository.CreateAsync(user);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TelegramId.Should().Be(user.TelegramId);
    }

    [Fact]
    public async Task CreateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.CreateAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.TelegramId.Should().Be(user.TelegramId);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingEntity_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_WithEntities_ReturnsAllEntities()
    {
        // Arrange
        var user1 = EntityBuilder.CreateUser(telegramId: 111);
        var user2 = EntityBuilder.CreateUser(telegramId: 222);
        var user3 = EntityBuilder.CreateUser(telegramId: 333);

        await _repository.CreateAsync(user1);
        await _repository.CreateAsync(user2);
        await _repository.CreateAsync(user3);

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(u => u.TelegramId == 111);
        result.Should().Contain(u => u.TelegramId == 222);
        result.Should().Contain(u => u.TelegramId == 333);
    }

    [Fact]
    public async Task UpdateAsync_ExistingEntity_UpdatesSuccessfully()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(username: "original");
        await _repository.CreateAsync(user);

        user.Username = "updated";

        // Act
        var result = await _repository.UpdateAsync(user);

        // Assert
        result.Username.Should().Be("updated");

        var retrievedUser = await _repository.GetByIdAsync(user.Id);
        retrievedUser!.Username.Should().Be("updated");
    }

    [Fact]
    public async Task UpdateAsync_NullEntity_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.DeleteAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        var deletedUser = await _repository.GetByIdAsync(user.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistingEntity_ReturnsFalse()
    {
        // Act
        var result = await _repository.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ExistingEntity_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.ExistsAsync(user.Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingEntity_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CountAsync_EmptyDatabase_ReturnsZero()
    {
        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task CountAsync_WithEntities_ReturnsCorrectCount()
    {
        // Arrange
        await _repository.CreateAsync(EntityBuilder.CreateUser(telegramId: 111));
        await _repository.CreateAsync(EntityBuilder.CreateUser(telegramId: 222));
        await _repository.CreateAsync(EntityBuilder.CreateUser(telegramId: 333));

        // Act
        var result = await _repository.CountAsync();

        // Assert
        result.Should().Be(3);
    }
}
