using FluentAssertions;
using SurveyBot.Infrastructure.Repositories;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Helpers;

namespace SurveyBot.Tests.Unit.Repositories;

public class UserRepositoryTests : RepositoryTestBase
{
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task GetByTelegramIdAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(telegramId: 12345);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByTelegramIdAsync(12345);

        // Assert
        result.Should().NotBeNull();
        result!.TelegramId.Should().Be(12345);
    }

    [Fact]
    public async Task GetByTelegramIdAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByTelegramIdAsync(99999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_ExistingUser_ReturnsUser()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(username: "testuser");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("testuser");
    }

    [Fact]
    public async Task GetByUsernameAsync_CaseInsensitive_ReturnsUser()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(username: "TestUser");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetByUsernameAsync("testuser");

        // Assert
        result.Should().NotBeNull();
        result!.Username.Should().Be("TestUser");
    }

    [Fact]
    public async Task GetByUsernameAsync_NonExistingUser_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("nonexistent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUsernameAsync_EmptyUsername_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByUsernameAsync("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByTelegramIdWithSurveysAsync_UserWithSurveys_ReturnsUserWithSurveys()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        var survey = EntityBuilder.CreateSurvey(creatorId: user.Id);
        await _context.Surveys.AddAsync(survey);
        await _context.SaveChangesAsync();

        var question = EntityBuilder.CreateQuestion(surveyId: survey.Id);
        await _context.Questions.AddAsync(question);
        await _context.SaveChangesAsync();

        var response = EntityBuilder.CreateResponse(surveyId: survey.Id);
        await _context.Responses.AddAsync(response);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByTelegramIdWithSurveysAsync(user.TelegramId);

        // Assert
        result.Should().NotBeNull();
        result!.Surveys.Should().HaveCount(1);
        result.Surveys.First().Questions.Should().HaveCount(1);
        result.Surveys.First().Responses.Should().HaveCount(1);
    }

    [Fact]
    public async Task ExistsByTelegramIdAsync_ExistingUser_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(telegramId: 12345);
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.ExistsByTelegramIdAsync(12345);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsByTelegramIdAsync_NonExistingUser_ReturnsFalse()
    {
        // Act
        var result = await _repository.ExistsByTelegramIdAsync(99999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUsernameTakenAsync_TakenUsername_ReturnsTrue()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(username: "takenuser");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.IsUsernameTakenAsync("takenuser");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsUsernameTakenAsync_AvailableUsername_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsUsernameTakenAsync("available");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsUsernameTakenAsync_EmptyUsername_ReturnsFalse()
    {
        // Act
        var result = await _repository.IsUsernameTakenAsync("");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateOrUpdateAsync_NewUser_CreatesUser()
    {
        // Act
        var result = await _repository.CreateOrUpdateAsync(
            telegramId: 12345,
            username: "newuser",
            firstName: "New",
            lastName: "User");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TelegramId.Should().Be(12345);
        result.Username.Should().Be("newuser");
        result.FirstName.Should().Be("New");
        result.LastName.Should().Be("User");
    }

    [Fact]
    public async Task CreateOrUpdateAsync_ExistingUser_UpdatesUser()
    {
        // Arrange
        var user = EntityBuilder.CreateUser(
            telegramId: 12345,
            username: "olduser",
            firstName: "Old",
            lastName: "Name");
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.CreateOrUpdateAsync(
            telegramId: 12345,
            username: "newuser",
            firstName: "New",
            lastName: "Name");

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(user.Id); // Same ID
        result.Username.Should().Be("newuser");
        result.FirstName.Should().Be("New");

        var allUsers = await _repository.GetAllAsync();
        allUsers.Should().HaveCount(1); // Still only one user
    }

    [Fact]
    public async Task GetSurveyCreatorsAsync_UsersWithSurveys_ReturnsOnlyCreators()
    {
        // Arrange
        var creator1 = EntityBuilder.CreateUser(telegramId: 111, username: "creator1");
        var creator2 = EntityBuilder.CreateUser(telegramId: 222, username: "creator2");
        var nonCreator = EntityBuilder.CreateUser(telegramId: 333, username: "noncreator");

        await _repository.CreateAsync(creator1);
        await _repository.CreateAsync(creator2);
        await _repository.CreateAsync(nonCreator);

        await _context.Surveys.AddAsync(EntityBuilder.CreateSurvey(creatorId: creator1.Id));
        await _context.Surveys.AddAsync(EntityBuilder.CreateSurvey(creatorId: creator2.Id));
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSurveyCreatorsAsync();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(u => u.Username == "creator1");
        result.Should().Contain(u => u.Username == "creator2");
        result.Should().NotContain(u => u.Username == "noncreator");
    }

    [Fact]
    public async Task GetSurveyCountAsync_UserWithSurveys_ReturnsCorrectCount()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        await _context.Surveys.AddRangeAsync(
            EntityBuilder.CreateSurvey(title: "Survey 1", creatorId: user.Id),
            EntityBuilder.CreateSurvey(title: "Survey 2", creatorId: user.Id),
            EntityBuilder.CreateSurvey(title: "Survey 3", creatorId: user.Id)
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetSurveyCountAsync(user.Id);

        // Assert
        result.Should().Be(3);
    }

    [Fact]
    public async Task GetSurveyCountAsync_UserWithoutSurveys_ReturnsZero()
    {
        // Arrange
        var user = EntityBuilder.CreateUser();
        await _repository.CreateAsync(user);

        // Act
        var result = await _repository.GetSurveyCountAsync(user.Id);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public async Task SearchByNameAsync_MatchingFirstName_ReturnsUsers()
    {
        // Arrange
        await _repository.CreateAsync(EntityBuilder.CreateUser(firstName: "John", lastName: "Doe"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(firstName: "Jane", lastName: "Smith"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(firstName: "Bob", lastName: "Johnson"));

        // Act
        var result = await _repository.SearchByNameAsync("John");

        // Assert
        result.Should().HaveCount(2); // John and Johnson
    }

    [Fact]
    public async Task SearchByNameAsync_MatchingUsername_ReturnsUsers()
    {
        // Arrange
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "admin123"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "user456"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "admin789"));

        // Act
        var result = await _repository.SearchByNameAsync("admin");

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(u => u.Username!.Contains("admin"));
    }

    [Fact]
    public async Task SearchByNameAsync_EmptySearchTerm_ReturnsAllUsers()
    {
        // Arrange
        await _repository.CreateAsync(EntityBuilder.CreateUser(telegramId: 111));
        await _repository.CreateAsync(EntityBuilder.CreateUser(telegramId: 222));

        // Act
        var result = await _repository.SearchByNameAsync("");

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAllAsync_Override_ReturnsSortedUsers()
    {
        // Arrange
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "charlie"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "alice"));
        await _repository.CreateAsync(EntityBuilder.CreateUser(username: "bob"));

        // Act
        var result = await _repository.GetAllAsync();

        // Assert
        result.Should().HaveCount(3);
        result.First().Username.Should().Be("alice");
        result.Last().Username.Should().Be("charlie");
    }
}
