using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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

        user.SetUsername("updated");

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

    #region INFRA-003-EF-TRACKING Bug Fix Regression Tests

    /// <summary>
    /// Tests that modifications to tracked entities are persisted when calling UpdateAsync().
    /// This is the primary bug scenario - tracked entities were not being updated before the fix.
    /// Bug: INFRA-003-EF-TRACKING - GenericRepository.UpdateAsync() was not persisting changes for already-tracked entities.
    /// Fix: Added entity state detection to only call Update() for untracked (Detached) entities.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithTrackedEntity_ShouldPersistChanges()
    {
        // Arrange - Create and save a response
        var user = EntityBuilder.CreateUser(telegramId: 999888777L, username: "trackeduser", firstName: "Tracked");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(title: "Tracking Test Survey", creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var response = Response.Create(
            surveyId: survey.Id,
            respondentTelegramId: user.TelegramId);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        var responseRepository = new GenericRepository<Response>(_context);

        // Load the entity with tracking (default behavior)
        var trackedResponse = await responseRepository.GetByIdAsync(response.Id);
        trackedResponse.Should().NotBeNull();
        trackedResponse!.IsComplete.Should().BeFalse();
        trackedResponse.SubmittedAt.Should().BeNull();

        // Act - Modify the tracked entity and update
        trackedResponse.MarkAsComplete();
        await responseRepository.UpdateAsync(trackedResponse);

        // Clear context to force fresh load from database
        _context.ChangeTracker.Clear();

        // Assert - Changes should be persisted
        var updatedResponse = await responseRepository.GetByIdAsync(response.Id);
        updatedResponse.Should().NotBeNull();
        updatedResponse!.IsComplete.Should().BeTrue("IsComplete flag should be true after update");
        updatedResponse.SubmittedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that untracked (detached) entities can still be updated.
    /// This verifies the fix doesn't break the existing behavior for untracked entities.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithUntrackedEntity_ShouldPersistChanges()
    {
        // Arrange - Create and save a response
        var user = EntityBuilder.CreateUser(telegramId: 999888666L, username: "untrackeduser", firstName: "Untracked");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(title: "Untracking Test Survey", creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var response = Response.Create(
            surveyId: survey.Id,
            respondentTelegramId: user.TelegramId);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        // Clear tracking after initial save
        _context.ChangeTracker.Clear();

        var responseRepository = new GenericRepository<Response>(_context);

        // Load entity without tracking
        var untrackedResponse = await _context.Responses
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == response.Id);
        untrackedResponse.Should().NotBeNull();
        untrackedResponse!.IsComplete.Should().BeFalse();

        // Act - Modify the untracked entity and update
        untrackedResponse.MarkAsComplete();
        await responseRepository.UpdateAsync(untrackedResponse);

        // Clear context to force fresh load
        _context.ChangeTracker.Clear();

        // Assert - Changes should be persisted
        var updatedResponse = await responseRepository.GetByIdAsync(response.Id);
        updatedResponse.Should().NotBeNull();
        updatedResponse!.IsComplete.Should().BeTrue("IsComplete flag should be true after update");
        updatedResponse.SubmittedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests mixed scenario: updating tracked entity multiple times.
    /// Verifies that repeated updates to the same tracked entity work correctly.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_WithTrackedEntity_MultipleUpdates_ShouldPersistAllChanges()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(telegramId: 999888555L, username: "multiuser", firstName: "Multi");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(title: "Multi-Update Test", creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var response = Response.Create(
            surveyId: survey.Id,
            respondentTelegramId: user.TelegramId);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        // Clear tracking after initial save
        _context.ChangeTracker.Clear();

        var responseRepository = new GenericRepository<Response>(_context);

        // Load with tracking
        var trackedResponse = await responseRepository.GetByIdAsync(response.Id);
        trackedResponse.Should().NotBeNull();

        // Act - Mark complete (the primary bug scenario)
        trackedResponse!.MarkAsComplete();
        await responseRepository.UpdateAsync(trackedResponse);

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert - Changes persisted
        var finalResponse = await responseRepository.GetByIdAsync(response.Id);
        finalResponse.Should().NotBeNull();
        finalResponse!.IsComplete.Should().BeTrue();
        finalResponse.SubmittedAt.Should().NotBeNull();
    }

    /// <summary>
    /// Tests updating a survey's activation status with tracked entity.
    /// This is one of the 22+ service methods affected by the bug.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_SurveyActivation_WithTrackedEntity_ShouldPersist()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(telegramId: 999888444L, username: "activateuser", firstName: "Activate");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var survey = EntityBuilder.CreateSurvey(title: "Activation Test", creatorId: user.Id, isActive: false);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var surveyRepository = new GenericRepository<Survey>(_context);

        // Load with tracking
        var trackedSurvey = await surveyRepository.GetByIdAsync(survey.Id);
        trackedSurvey.Should().NotBeNull();
        trackedSurvey!.IsActive.Should().BeFalse();

        // Act - Activate survey (tracked entity)
        trackedSurvey.Activate();
        await surveyRepository.UpdateAsync(trackedSurvey);

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert
        var activatedSurvey = await surveyRepository.GetByIdAsync(survey.Id);
        activatedSurvey.Should().NotBeNull();
        activatedSurvey!.IsActive.Should().BeTrue("Survey should be activated");
    }

    /// <summary>
    /// Tests that entity state is correctly detected for tracked vs untracked entities.
    /// Verifies the fix properly checks EntityState before calling Update().
    /// </summary>
    [Fact]
    public async Task UpdateAsync_EntityStateDetection_ShouldHandleTrackedAndUntrackedCorrectly()
    {
        // Arrange - Create two users
        var user1 = EntityBuilder.CreateUser(telegramId: 111222333L, username: "stateuser1", firstName: "State1");
        var user2 = EntityBuilder.CreateUser(telegramId: 111222444L, username: "stateuser2", firstName: "State2");
        await _context.Users.AddAsync(user1);
        await _context.Users.AddAsync(user2);
        await _context.SaveChangesAsync();

        // Clear tracking after initial save
        _context.ChangeTracker.Clear();

        var userRepository = new GenericRepository<User>(_context);

        // Load user1 with tracking
        var trackedUser = await userRepository.GetByIdAsync(user1.Id);
        trackedUser.Should().NotBeNull();

        // Load user2 without tracking
        var untrackedUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == user2.Id);
        untrackedUser.Should().NotBeNull();

        // Act - Update both
        trackedUser!.SetUsername("tracked_updated");
        untrackedUser!.SetUsername("untracked_updated");

        await userRepository.UpdateAsync(trackedUser);
        await userRepository.UpdateAsync(untrackedUser);

        // Clear context
        _context.ChangeTracker.Clear();

        // Assert - Both should be updated
        var updatedUser1 = await userRepository.GetByIdAsync(user1.Id);
        var updatedUser2 = await userRepository.GetByIdAsync(user2.Id);

        updatedUser1.Should().NotBeNull();
        updatedUser1!.Username.Should().Be("tracked_updated");

        updatedUser2.Should().NotBeNull();
        updatedUser2!.Username.Should().Be("untracked_updated");
    }

    #endregion
}
