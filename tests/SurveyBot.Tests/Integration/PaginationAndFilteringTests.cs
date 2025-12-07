using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using SurveyBot.API.Models;
using SurveyBot.Core.DTOs.Auth;
using SurveyBot.Core.DTOs.Common;
using SurveyBot.Core.DTOs.Response;
using SurveyBot.Core.DTOs.Survey;
using SurveyBot.Core.Entities;
using SurveyBot.Tests.Fixtures;
using SurveyBot.Tests.Infrastructure;

namespace SurveyBot.Tests.Integration;

/// <summary>
/// Integration tests for pagination and filtering functionality.
/// Tests pagination, search, and filtering across surveys and responses.
/// </summary>
public class PaginationAndFilteringTests : IntegrationTestBase
{
    public PaginationAndFilteringTests(WebApplicationFactoryFixture<Program> factory)
        : base(factory)
    {
    }

    [Fact]
    public async Task SurveyList_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            // Create 15 surveys
            for (int i = 1; i <= 15; i++)
            {
                db.Surveys.Add(EntityBuilder.CreateSurvey(
                    title: $"Survey {i}",
                    creatorId: user.Id));
            }
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Get first page (10 items)
        var response1 = await Client.GetAsync("/api/surveys?pageNumber=1&pageSize=10");
        var result1 = await response1.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyDto>>>();

        // Act - Get second page
        var response2 = await Client.GetAsync("/api/surveys?pageNumber=2&pageSize=10");
        var result2 = await response2.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyDto>>>();

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.OK);
        result1!.Data!.Items.Should().HaveCount(10);
        result1.Data.TotalCount.Should().Be(15);
        result1.Data.TotalPages.Should().Be(2);
        result1.Data.PageNumber.Should().Be(1);

        response2.StatusCode.Should().Be(HttpStatusCode.OK);
        result2!.Data!.Items.Should().HaveCount(5);
        result2.Data.PageNumber.Should().Be(2);
    }

    [Fact]
    public async Task SurveyList_SearchByTitle_ReturnsMatchingSurveys()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Customer Satisfaction Survey", creatorId: user.Id));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Employee Feedback", creatorId: user.Id));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Product Customer Review", creatorId: user.Id));
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Search for "Customer"
        var response = await Client.GetAsync("/api/surveys?search=Customer");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyDto>>>();
        result!.Data!.Items.Should().HaveCount(2);
        result.Data.Items.Should().OnlyContain(s => s.Title.Contains("Customer"));
    }

    [Fact]
    public async Task SurveyList_FilterByStatus_ReturnsFilteredSurveys()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Active Survey 1", creatorId: user.Id, isActive: true));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Active Survey 2", creatorId: user.Id, isActive: true));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Inactive Survey 1", creatorId: user.Id, isActive: false));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Inactive Survey 2", creatorId: user.Id, isActive: false));
            db.Surveys.Add(EntityBuilder.CreateSurvey(title: "Inactive Survey 3", creatorId: user.Id, isActive: false));
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Filter active surveys
        var activeResponse = await Client.GetAsync("/api/surveys?isActive=true");
        var activeResult = await activeResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyDto>>>();

        // Act - Filter inactive surveys
        var inactiveResponse = await Client.GetAsync("/api/surveys?isActive=false");
        var inactiveResult = await inactiveResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<SurveyDto>>>();

        // Assert
        activeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        activeResult!.Data!.Items.Should().HaveCount(2);
        activeResult.Data.Items.Should().OnlyContain(s => s.IsActive);

        inactiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        inactiveResult!.Data!.Items.Should().HaveCount(3);
        inactiveResult.Data.Items.Should().OnlyContain(s => !s.IsActive);
    }

    [Fact]
    public async Task ResponseList_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        int surveyId = 0;

        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            var survey = EntityBuilder.CreateSurvey(creatorId: user.Id, isActive: true);
            db.Surveys.Add(survey);
            db.SaveChanges();
            surveyId = survey.Id;

            // Create 25 responses
            for (int i = 1; i <= 25; i++)
            {
                db.Responses.Add(EntityBuilder.CreateResponse(
                    surveyId: survey.Id,
                    respondentTelegramId: 900000000 + i,
                    isComplete: i % 2 == 0)); // Even numbered are complete
            }
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Get first page with completedOnly filter
        var response = await Client.GetAsync($"/api/surveys/{surveyId}/responses?pageNumber=1&pageSize=10&completedOnly=true");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResultDto<ResponseDto>>>();
        result!.Data!.Items.Should().HaveCount(10);
        result.Data.Items.Should().OnlyContain(r => r.IsComplete);
        result.Data.TotalCount.Should().Be(12); // 12 complete responses out of 25
    }

    [Fact]
    public async Task Pagination_WithInvalidParameters_ReturnsBadRequest()
    {
        // Arrange
        // Database is already cleared by IntegrationTestBase constructor
        SeedDatabase(db =>
        {
            var user = EntityBuilder.CreateUser(telegramId: 123456789);
            db.Users.Add(user);
            db.SaveChanges();

            db.Surveys.Add(EntityBuilder.CreateSurvey(creatorId: user.Id));
        });

        var token = await GetAuthTokenAsync();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Act - Invalid page number (0)
        var response1 = await Client.GetAsync("/api/surveys?pageNumber=0&pageSize=10");

        // Act - Invalid page size (too large)
        var response2 = await Client.GetAsync("/api/surveys?pageNumber=1&pageSize=200");

        // Act - Negative page number
        var response3 = await Client.GetAsync("/api/surveys?pageNumber=-1&pageSize=10");

        // Assert
        response1.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response2.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response3.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
