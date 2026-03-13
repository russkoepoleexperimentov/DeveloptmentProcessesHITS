using System.Net.Http.Headers;
using Application.DTOs.Auth;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.Models; 
using Web;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

public class CourseIntegrationTests : IClassFixture<WebApplicationFactory<Web.Program>>
{
    private readonly WebApplicationFactory<Web.Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CourseIntegrationTests(WebApplicationFactory<Web.Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private async Task<(HttpClient Client, string AccessToken)> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var email = $"test{Guid.NewGuid()}@test.com";
        var password = "Test123!";
        var credentials = $"user{Guid.NewGuid():N}"[..10];

        var registerDto = new UserRegisterDto
        {
            Email = email,
            Password = password,
            Credentials = credentials
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(loginJson, _jsonOptions);

        // Добавляем токен в заголовки
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data.AccessToken);

        return (client, loginResult.Data.AccessToken);
    }

    [Fact]
    public async Task CreateCourse_GetDetails_UpdateCourse_ShouldWork()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        // 1. Создание курса (без Description, так как его нет в DTO)
        var createDto = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
            // Description отсутствует в DTO
        };

        var createResponse = await client.PostAsJsonAsync("/api/course", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<CreateUpdateCourseResponseDto>>(createJson, _jsonOptions);

        createResult.Should().NotBeNull();
        createResult!.Type.Should().Be(ApiResponseType.Success);
        createResult.Data.Should().NotBeNull();
        createResult.Data.Id.Should().NotBeEmpty();
        createResult.Data.Title.Should().Be("Test Course");

        var courseId = createResult.Data.Id;

        // 2. Получение деталей курса
        var getResponse = await client.GetAsync($"/api/course/{courseId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<ApiResponse<CourseDetailsDto>>(getJson, _jsonOptions);

        getResult.Should().NotBeNull();
        getResult!.Type.Should().Be(ApiResponseType.Success);
        getResult.Data.Should().NotBeNull();
        getResult.Data.Id.Should().Be(courseId);
        getResult.Data.Title.Should().Be("Test Course");

        // 3. Обновление курса (без Description)
        var updateDto = new CreateUpdateCourseRequestDto
        {
            Title = "Updated Course"
            // Description отсутствует в DTO
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/course/{courseId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadAsStringAsync();
        var updateResult = JsonSerializer.Deserialize<ApiResponse<CreateUpdateCourseResponseDto>>(updateJson, _jsonOptions);

        updateResult.Should().NotBeNull();
        updateResult!.Type.Should().Be(ApiResponseType.Success);
        updateResult.Data.Title.Should().Be("Updated Course");
    }

    [Fact]
    public async Task GetCourseMembers_ShouldWork()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        // Создаем курс
        var createDto = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
        };

        var createResponse = await client.PostAsJsonAsync("/api/course", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<CreateUpdateCourseResponseDto>>(createJson, _jsonOptions);
        var courseId = createResult!.Data.Id;

        // Получаем участников курса
        var membersResponse = await client.GetAsync($"/api/course/{courseId}/members?skip=0&take=10");
        membersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var membersJson = await membersResponse.Content.ReadAsStringAsync();
        var membersResult = JsonSerializer.Deserialize<ApiResponse<PagedResponse<CourseMemberDto>>>(membersJson, _jsonOptions);

        membersResult.Should().NotBeNull();
        membersResult!.Type.Should().Be(ApiResponseType.Success);
        membersResult.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task JoinCourse_WithInviteCode_ShouldWork()
    {
        // Arrange
        var (client, _) = await CreateAuthenticatedClient();

        // Создаем курс (получаем invite code)
        var createDto = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
        };

        var createResponse = await client.PostAsJsonAsync("/api/course", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<CreateUpdateCourseResponseDto>>(createJson, _jsonOptions);

        // Получаем invite code из деталей курса
        var getResponse = await client.GetAsync($"/api/course/{createResult!.Data.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<ApiResponse<CourseDetailsDto>>(getJson, _jsonOptions);

        var inviteCode = getResult!.Data.InviteCode;

        // Создаем второго пользователя для присоединения
        var secondClient = _factory.CreateClient();
        var secondEmail = $"test{Guid.NewGuid()}@test.com";
        var secondCredentials = $"user{Guid.NewGuid():N}"[..10];

        var secondRegisterDto = new UserRegisterDto
        {
            Email = secondEmail,
            Password = "Test123!",
            Credentials = secondCredentials
        };

        await secondClient.PostAsJsonAsync("/api/auth/register", secondRegisterDto);

        var secondLoginDto = new UserLoginDto
        {
            Email = secondEmail,
            Password = "Test123!"
        };

        var secondLoginResponse = await secondClient.PostAsJsonAsync("/api/auth/login", secondLoginDto);
        var secondLoginJson = await secondLoginResponse.Content.ReadAsStringAsync();
        var secondLoginResult = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(secondLoginJson, _jsonOptions);

        secondClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", secondLoginResult!.Data.AccessToken);

        var joinDto = new JoinCourseRequestDto
        {
            InviteCode = inviteCode!
        };

        var joinResponse = await secondClient.PostAsJsonAsync("/api/course/join", joinDto);
        joinResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var joinJson = await joinResponse.Content.ReadAsStringAsync();
        var joinResult = JsonSerializer.Deserialize<ApiResponse<JoinCourseResponseDto>>(joinJson, _jsonOptions);

        joinResult.Should().NotBeNull();
        joinResult!.Type.Should().Be(ApiResponseType.Success);
        joinResult.Data.Id.Should().Be(createResult.Data.Id);
    }
}