using Application.DTOs.Auth;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Common;
using Web;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace IntegrationTests;

public class AuthIntegrationTests : IClassFixture<WebApplicationFactory<Web.Program>>
{
    private readonly WebApplicationFactory<Web.Program> _factory;
    private readonly JsonSerializerOptions _jsonOptions;

    public AuthIntegrationTests(WebApplicationFactory<Web.Program> factory)
    {
        _factory = factory;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    [Fact]
    public async Task Register_Login_Refresh_ShouldWork()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"test{Guid.NewGuid()}@test.com";
        var password = "Test123!";
        var credentials = $"user{Guid.NewGuid():N}"[..10]; // Генерируем уникальные credentials

        var registerDto = new UserRegisterDto
        {
            Email = email,
            Password = password,
            Credentials = credentials
        };

        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerDto);


        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var registerJson = await registerResponse.Content.ReadAsStringAsync();
        var registerResult = JsonSerializer.Deserialize<ApiResponse<object>>(registerJson, _jsonOptions);

        registerResult.Should().NotBeNull();
        registerResult!.Type.Should().Be(ApiResponseType.Success);

        var loginDto = new UserLoginDto
        {
            Email = email,
            Password = password
        };

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", loginDto);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginJson = await loginResponse.Content.ReadAsStringAsync();
        var loginResult = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(loginJson, _jsonOptions);

        loginResult.Should().NotBeNull();
        loginResult!.Type.Should().Be(ApiResponseType.Success);
        loginResult.Data.Should().NotBeNull();
        loginResult.Data.AccessToken.Should().NotBeNullOrEmpty();
        loginResult.Data.RefreshToken.Should().NotBeNullOrEmpty();

        var refreshToken = loginResult.Data.RefreshToken;

        var refreshResponse = await client.PostAsync($"/api/auth/refresh?token={refreshToken}", null);
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshJson = await refreshResponse.Content.ReadAsStringAsync();
        var refreshResult = JsonSerializer.Deserialize<ApiResponse<TokenResponse>>(refreshJson, _jsonOptions);

        refreshResult.Should().NotBeNull();
        refreshResult!.Type.Should().Be(ApiResponseType.Success);
        refreshResult.Data.Should().NotBeNull();
        refreshResult.Data.AccessToken.Should().NotBeNullOrEmpty();
        refreshResult.Data.RefreshToken.Should().NotBeNullOrEmpty();
    }
}