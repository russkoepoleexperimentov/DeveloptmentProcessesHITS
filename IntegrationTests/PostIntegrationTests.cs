using System.Net.Http.Headers;
using Application.DTOs.Auth;
using Application.DTOs.Post;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.DTOs.Post; 
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

public class PostIntegrationTests : IClassFixture<WebApplicationFactory<Web.Program>>
{
    private readonly WebApplicationFactory<Web.Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public PostIntegrationTests(WebApplicationFactory<Web.Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private async Task<(HttpClient Client, string AccessToken, Guid CourseId)> CreateAuthenticatedClientWithCourse()
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

        await client.PostAsJsonAsync("/api/auth/register", registerDto);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(loginJson, _jsonOptions);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", loginResult!.Data.AccessToken);

        // Создаем курс
        var courseDto = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
        };

        var courseResponse = await client.PostAsJsonAsync("/api/course", courseDto);
        var courseJson = await courseResponse.Content.ReadAsStringAsync();
        var courseResult = JsonSerializer.Deserialize<ApiResponse<CreateUpdateCourseResponseDto>>(courseJson, _jsonOptions);
        var courseId = courseResult!.Data.Id;

        return (client, loginResult.Data.AccessToken, courseId);
    }

    [Fact]
    public async Task CreatePost_GetPost_UpdatePost_DeletePost_ShouldWork()
    {
        // Arrange
        var (client, _, courseId) = await CreateAuthenticatedClientWithCourse();

        // 1. Создание поста
        var createDto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Test Post",
            Text = "Test Content"
        };

        var createResponse = await client.PostAsJsonAsync($"/api/course/{courseId}/task", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(createJson, _jsonOptions);

        createResult.Should().NotBeNull();
        createResult!.Type.Should().Be(ApiResponseType.Success);
        createResult.Data.Id.Should().NotBeEmpty();

        var postId = createResult.Data.Id;

        // 2. Получение поста
        var getResponse = await client.GetAsync($"/api/post/{postId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<ApiResponse<PostDetailsDto>>(getJson, _jsonOptions);

        getResult.Should().NotBeNull();
        getResult!.Type.Should().Be(ApiResponseType.Success);
        getResult.Data.Id.Should().Be(postId);
        getResult.Data.Title.Should().Be("Test Post");

        var updateDto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Updated Post",
            Text = "Updated Content"
        };

        var updateResponse = await client.PutAsJsonAsync($"/api/post/{postId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateJson = await updateResponse.Content.ReadAsStringAsync();
        var updateResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(updateJson, _jsonOptions);

        updateResult.Should().NotBeNull();
        updateResult!.Type.Should().Be(ApiResponseType.Success);
        updateResult.Data.Id.Should().Be(postId);

        var deleteResponse = await client.DeleteAsync($"/api/post/{postId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteJson = await deleteResponse.Content.ReadAsStringAsync();
        var deleteResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(deleteJson, _jsonOptions);

        deleteResult.Should().NotBeNull();
        deleteResult!.Type.Should().Be(ApiResponseType.Success);
        deleteResult.Data.Id.Should().Be(postId);
    }

    [Fact]
    public async Task GetCourseFeed_ShouldWork()
    {
        // Arrange
        var (client, _, courseId) = await CreateAuthenticatedClientWithCourse();

        // Создаем несколько постов
        for (int i = 0; i < 3; i++)
        {
            var postDto = new CreateUpdatePostDto
            {
                Type = PostType.POST,
                Title = $"Post {i}",
                Text = $"Content {i}"
            };

            await client.PostAsJsonAsync($"/api/course/{courseId}/task", postDto);
        }

        // Получаем ленту
        var feedResponse = await client.GetAsync($"/api/course/{courseId}/feed?skip=0&take=10");
        feedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var feedJson = await feedResponse.Content.ReadAsStringAsync();
        var feedResult = JsonSerializer.Deserialize<ApiResponse<FeedResponseDto>>(feedJson, _jsonOptions);

        feedResult.Should().NotBeNull();
        feedResult!.Type.Should().Be(ApiResponseType.Success);
        feedResult.Data.Should().NotBeNull();
        feedResult.Data.TotalRecords.Should().Be(3);
        feedResult.Data.Records.Should().HaveCount(3);
    }
}