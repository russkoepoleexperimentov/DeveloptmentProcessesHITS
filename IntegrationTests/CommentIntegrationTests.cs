using System.Net.Http.Headers;
using Application.DTOs.Auth;
using Application.DTOs.Post; 
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Comment;
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

public class CommentIntegrationTests : IClassFixture<WebApplicationFactory<Web.Program>>
{
    private readonly WebApplicationFactory<Web.Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public CommentIntegrationTests(WebApplicationFactory<Web.Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    private async Task<(HttpClient Client, string AccessToken, Guid CourseId, Guid PostId)> CreateAuthenticatedClientWithPost()
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

        // Создаем пост
        var postDto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Test Post",
            Text = "Test Content"
        };

        var postResponse = await client.PostAsJsonAsync($"/api/course/{courseId}/task", postDto);
        var postJson = await postResponse.Content.ReadAsStringAsync();
        var postResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(postJson, _jsonOptions);
        var postId = postResult!.Data.Id;

        return (client, loginResult.Data.AccessToken, courseId, postId);
    }

    [Fact]
    public async Task CreateComment_GetReplies_DeleteComment_ShouldWork()
    {
        // Arrange
        var (client, _, _, postId) = await CreateAuthenticatedClientWithPost();

        // 1. Создание комментария
        var createDto = new AddCommentRequestDto
        {
            Text = "Test comment"
        };

        var createResponse = await client.PostAsJsonAsync($"/api/post/{postId}/comment", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var createJson = await createResponse.Content.ReadAsStringAsync();
        var createResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(createJson, _jsonOptions);

        createResult.Should().NotBeNull();
        createResult!.Type.Should().Be(ApiResponseType.Success);
        createResult.Data.Id.Should().NotBeEmpty();

        var commentId = createResult.Data.Id;

        // 2. Получение комментариев к посту
        var getResponse = await client.GetAsync($"/api/post/{postId}/comment");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getJson = await getResponse.Content.ReadAsStringAsync();
        var getResult = JsonSerializer.Deserialize<ApiResponse<List<CommentDto>>>(getJson, _jsonOptions);

        getResult.Should().NotBeNull();
        getResult!.Type.Should().Be(ApiResponseType.Success);
        getResult.Data.Should().NotBeEmpty();

        // 3. Удаление комментария
        var deleteResponse = await client.DeleteAsync($"/api/comment/{commentId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deleteJson = await deleteResponse.Content.ReadAsStringAsync();
        var deleteResult = JsonSerializer.Deserialize<ApiResponse<IdRequestDto>>(deleteJson, _jsonOptions);

        deleteResult.Should().NotBeNull();
        deleteResult!.Type.Should().Be(ApiResponseType.Success);
        deleteResult.Data.Id.Should().Be(commentId);
    }
}