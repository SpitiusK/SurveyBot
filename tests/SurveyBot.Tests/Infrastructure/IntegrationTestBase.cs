using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Tests.Fixtures;

namespace SurveyBot.Tests.Infrastructure;

/// <summary>
/// Base class for integration tests that provides complete test isolation with instance-per-test factory.
/// Each test method gets its own WebApplicationFactory, TestServer, ServiceProvider, and database.
/// </summary>
/// <remarks>
/// This base class addresses multiple test isolation issues:
/// - TEST-FAIL-001: Database State Pollution - Each test gets a unique database
/// - TEST-FAIL-002: HttpClient Header Pollution - Each test gets a fresh HttpClient
/// - TEST-FLAKY-AUTH-003 (Phase 2): Factory/TestServer Isolation - Each test gets its own factory instance
///
/// By implementing IAsyncLifetime, each test method creates and disposes its own factory,
/// ensuring complete isolation of TestServer, ServiceProvider, and JWT middleware state.
/// </remarks>
public abstract class IntegrationTestBase : IAsyncLifetime
{
    private WebApplicationFactoryFixture<Program>? _factory;
    private HttpClient? _client;

    /// <summary>
    /// Gets the WebApplicationFactory for the current test. Created during InitializeAsync().
    /// Each test method receives its own factory instance for complete isolation.
    /// </summary>
    protected WebApplicationFactoryFixture<Program> Factory => _factory!;

    /// <summary>
    /// Gets the HttpClient for the current test. Created during InitializeAsync().
    /// Each test method receives a fresh HttpClient with no shared state.
    /// </summary>
    protected HttpClient Client => _client!;

    /// <summary>
    /// Creates a new HttpClient instance for the test.
    /// Virtual to allow derived classes to customize client creation if needed.
    /// </summary>
    protected virtual HttpClient CreateClient()
    {
        return Factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    /// <summary>
    /// Called before each test method runs. Creates a new factory, TestServer, HttpClient, and database.
    /// </summary>
    /// <remarks>
    /// TEST-FLAKY-AUTH-003 (Phase 2): Creates a NEW factory instance per test method for complete isolation.
    /// TEST-PARALLEL-001 (Phase 3): Calls ResetDatabaseName() to ensure each test method
    /// gets a unique database, enabling safe parallel test execution without race conditions.
    /// </remarks>
    public Task InitializeAsync()
    {
        // Create NEW factory per test (instance-per-test pattern)
        // This ensures complete isolation of TestServer, ServiceProvider, and JWT middleware state
        _factory = new WebApplicationFactoryFixture<Program>();

        // Fix TEST-PARALLEL-001 (Phase 3): Reset database name BEFORE server starts
        // This ensures the server and all subsequent operations use the same unique database
        _factory.ResetDatabaseName();

        // Ensure server is started AFTER database name is set
        // This ensures ConfigureServices uses the correct database name from AsyncLocal
        _factory.EnsureServerStarted();

        // Clear database before each test to ensure isolation
        // Now uses the unique database name set above
        ClearDatabase();

        // Create client AFTER factory setup
        _client = CreateClient();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after each test method completes. Cleans up HttpClient, TestServer, and factory resources.
    /// </summary>
    /// <remarks>
    /// TEST-FLAKY-AUTH-003 (Phase 2): Disposes the factory to clean up TestServer and ServiceProvider.
    /// This ensures no shared state leaks between test methods.
    /// </remarks>
    public async Task DisposeAsync()
    {
        // Clear authorization header (defensive cleanup)
        if (_client != null)
        {
            _client.DefaultRequestHeaders.Authorization = null;
            _client.DefaultRequestHeaders.Clear();
        }

        // Dispose HttpClient
        _client?.Dispose();
        _client = null;

        // CRITICAL: Dispose factory to clean up TestServer and ServiceProvider
        // This prevents Authorization header pollution and JWT middleware state leakage
        if (_factory != null)
        {
            await _factory.DisposeAsync();
            _factory = null;
        }
    }

    /// <summary>
    /// Clears all data from the database to ensure test isolation.
    /// </summary>
    protected void ClearDatabase()
    {
        Factory.ClearDatabase();
    }

    /// <summary>
    /// Seeds the database with test data using the provided action.
    /// </summary>
    protected void SeedDatabase(Action<SurveyBot.Infrastructure.Data.SurveyBotDbContext> seedAction)
    {
        Factory.SeedDatabase(seedAction);
    }

    /// <summary>
    /// Gets an authentication token for test requests with proper null-safety.
    /// Throws descriptive exceptions if login fails instead of NullReferenceException.
    /// </summary>
    /// <param name="telegramId">The Telegram ID to use for login. Defaults to 123456789.</param>
    /// <returns>The JWT token for authentication.</returns>
    /// <exception cref="InvalidOperationException">Thrown when login fails or token is null.</exception>
    /// <remarks>
    /// This method addresses TEST-FAIL-001: Null-Safety in Test Helpers issue.
    /// It validates HTTP status code and response structure before accessing the token.
    /// </remarks>
    protected async Task<string> GetAuthTokenAsync(long telegramId = 123456789)
    {
        var loginRequest = new LoginRequestDto { TelegramId = telegramId };
        var response = await Client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // 1. Validate HTTP status code
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(
                $"Login failed with status {response.StatusCode}. " +
                $"Telegram ID: {telegramId}. " +
                $"Response: {errorContent}");
        }

        // 2. Deserialize with null-safety
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponseDto>>();

        // 3. Validate response structure
        if (result == null)
        {
            throw new InvalidOperationException(
                $"Login response deserialization returned null for Telegram ID: {telegramId}");
        }

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Login failed: {result.Message} for Telegram ID: {telegramId}");
        }

        if (result.Data?.Token == null)
        {
            throw new InvalidOperationException(
                $"Login succeeded but token is null for Telegram ID: {telegramId}");
        }

        return result.Data.Token;
    }

    /// <summary>
    /// Sets the authorization header with a JWT token for subsequent requests.
    /// </summary>
    protected async Task AuthenticateAsync(long telegramId = 123456789)
    {
        var token = await GetAuthTokenAsync(telegramId);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    /// <summary>
    /// Clears the authorization header.
    /// </summary>
    protected void ClearAuthentication()
    {
        Client.DefaultRequestHeaders.Authorization = null;
    }
}
