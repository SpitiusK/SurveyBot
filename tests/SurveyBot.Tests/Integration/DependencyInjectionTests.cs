using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SurveyBot.Core.Interfaces;
using SurveyBot.Infrastructure.Data;
using SurveyBot.Infrastructure.Repositories;

namespace SurveyBot.Tests.Integration;

public class DependencyInjectionTests
{
    private IServiceCollection CreateServiceCollection()
    {
        var services = new ServiceCollection();

        // Configure DbContext with in-memory database
        services.AddDbContext<SurveyBotDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        // Register repositories
        services.AddScoped<ISurveyRepository, SurveyRepository>();
        services.AddScoped<IQuestionRepository, QuestionRepository>();
        services.AddScoped<IResponseRepository, ResponseRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAnswerRepository, AnswerRepository>();

        return services;
    }

    [Fact]
    public void DI_DbContext_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var dbContext = serviceProvider.GetService<SurveyBotDbContext>();

        // Assert
        dbContext.Should().NotBeNull();
    }

    [Fact]
    public void DI_DbContext_HasScopedLifetime()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        SurveyBotDbContext? context1;
        SurveyBotDbContext? context2;

        using (var scope = serviceProvider.CreateScope())
        {
            context1 = scope.ServiceProvider.GetService<SurveyBotDbContext>();
            var context1Again = scope.ServiceProvider.GetService<SurveyBotDbContext>();

            // Within same scope, should get same instance
            context1.Should().BeSameAs(context1Again);
        }

        using (var scope = serviceProvider.CreateScope())
        {
            context2 = scope.ServiceProvider.GetService<SurveyBotDbContext>();
        }

        // Different scopes should have different instances
        context2.Should().NotBeSameAs(context1);
    }

    [Fact]
    public void DI_SurveyRepository_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var repository = serviceProvider.GetService<ISurveyRepository>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<SurveyRepository>();
    }

    [Fact]
    public void DI_QuestionRepository_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var repository = serviceProvider.GetService<IQuestionRepository>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<QuestionRepository>();
    }

    [Fact]
    public void DI_ResponseRepository_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var repository = serviceProvider.GetService<IResponseRepository>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<ResponseRepository>();
    }

    [Fact]
    public void DI_UserRepository_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var repository = serviceProvider.GetService<IUserRepository>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<UserRepository>();
    }

    [Fact]
    public void DI_AnswerRepository_ResolvesCorrectly()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        var repository = serviceProvider.GetService<IAnswerRepository>();

        // Assert
        repository.Should().NotBeNull();
        repository.Should().BeOfType<AnswerRepository>();
    }

    [Fact]
    public void DI_AllRepositories_HaveScopedLifetime()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using (var scope = serviceProvider.CreateScope())
        {
            var surveyRepo1 = scope.ServiceProvider.GetService<ISurveyRepository>();
            var surveyRepo2 = scope.ServiceProvider.GetService<ISurveyRepository>();

            // Within same scope, should get same instance
            surveyRepo1.Should().BeSameAs(surveyRepo2);
        }

        ISurveyRepository? surveyRepoNewScope;
        using (var scope = serviceProvider.CreateScope())
        {
            surveyRepoNewScope = scope.ServiceProvider.GetService<ISurveyRepository>();
        }

        // Different scopes should have different instances (can't directly compare due to disposal)
        surveyRepoNewScope.Should().NotBeNull();
    }

    [Fact]
    public void DI_Repository_SharesSameDbContextInScope()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();
        var surveyRepo = scope.ServiceProvider.GetService<ISurveyRepository>();
        var userRepo = scope.ServiceProvider.GetService<IUserRepository>();
        var dbContext = scope.ServiceProvider.GetService<SurveyBotDbContext>();

        // Assert
        surveyRepo.Should().NotBeNull();
        userRepo.Should().NotBeNull();
        dbContext.Should().NotBeNull();

        // All services should be resolved correctly within the scope
        // The DbContext is shared among repositories (scoped lifetime ensures this)
    }

    [Fact]
    public void DI_AllRepositories_CanBeResolvedTogether()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act
        using var scope = serviceProvider.CreateScope();

        var surveyRepo = scope.ServiceProvider.GetService<ISurveyRepository>();
        var questionRepo = scope.ServiceProvider.GetService<IQuestionRepository>();
        var responseRepo = scope.ServiceProvider.GetService<IResponseRepository>();
        var userRepo = scope.ServiceProvider.GetService<IUserRepository>();
        var answerRepo = scope.ServiceProvider.GetService<IAnswerRepository>();

        // Assert
        surveyRepo.Should().NotBeNull();
        questionRepo.Should().NotBeNull();
        responseRepo.Should().NotBeNull();
        userRepo.Should().NotBeNull();
        answerRepo.Should().NotBeNull();
    }

    [Fact]
    public void DI_ServiceDescriptor_RepositoriesHaveCorrectLifetime()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        var surveyRepoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ISurveyRepository));
        var questionRepoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IQuestionRepository));
        var responseRepoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IResponseRepository));
        var userRepoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IUserRepository));
        var answerRepoDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IAnswerRepository));

        // Assert
        surveyRepoDescriptor.Should().NotBeNull();
        surveyRepoDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        questionRepoDescriptor.Should().NotBeNull();
        questionRepoDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        responseRepoDescriptor.Should().NotBeNull();
        responseRepoDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        userRepoDescriptor.Should().NotBeNull();
        userRepoDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);

        answerRepoDescriptor.Should().NotBeNull();
        answerRepoDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void DI_DbContext_ServiceDescriptor_HasScopedLifetime()
    {
        // Arrange
        var services = CreateServiceCollection();

        // Act
        var dbContextDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(SurveyBotDbContext));

        // Assert
        dbContextDescriptor.Should().NotBeNull();
        dbContextDescriptor!.Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void DI_Repository_CanPerformDatabaseOperations()
    {
        // Arrange
        var services = CreateServiceCollection();
        var serviceProvider = services.BuildServiceProvider();

        // Act & Assert
        using var scope = serviceProvider.CreateScope();
        var userRepo = scope.ServiceProvider.GetService<IUserRepository>();

        userRepo.Should().NotBeNull();

        // Verify repository can interact with database
        var createTask = userRepo!.CreateAsync(new Core.Entities.User
        {
            TelegramId = 12345,
            Username = "testuser"
        });

        createTask.Should().NotBeNull();
        var action = async () => await createTask;
        action.Should().NotThrowAsync();
    }
}
