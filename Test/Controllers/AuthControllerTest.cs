using Application.DTOs.Auth;
using Application.Services.Interfaces;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Auth;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly AuthController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<IAuthService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("sub", _userId.ToString()),
            new Claim("id", _userId.ToString())
        };

        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new AuthController(_authServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task Register_ShouldReturnOkWithTokenResponse()
    {
        // Arrange
        var dto = new UserRegisterDto
        {
            Email = "test@test.com",
            Password = "password123"
        };

        var expectedResult = new TokenResponse
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token"
        };

        _authServiceMock.Setup(x => x.RegisterAsync(dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Register(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();
    }

    [Fact]
    public async Task Login_ShouldReturnOkWithTokenResponse()
    {
        // Arrange
        var dto = new UserLoginDto
        {
            Email = "test@test.com",
            Password = "password123"
        };

        var expectedResult = new TokenResponse
        {
            AccessToken = "access_token",
            RefreshToken = "refresh_token"
        };

        _authServiceMock.Setup(x => x.LoginAsync(dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Login(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task Logout_ShouldReturnOk_WhenUserAuthorized()
    {
        // Arrange
        _authServiceMock.Setup(x => x.LogoutAsync(_userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Logout();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();

        _authServiceMock.Verify(x => x.LogoutAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldReturnOkWithNewTokens()
    {
        // Arrange
        var token = "old_refresh_token";
        var expectedResult = new TokenResponse
        {
            AccessToken = "new_access_token",
            RefreshToken = "new_refresh_token"
        };

        _authServiceMock.Setup(x => x.RefreshAsync(token))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.Refresh(token);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
    }

    [Fact]
    public async Task ChangePassword_ShouldReturnOk_WhenUserAuthorized()
    {
        // Arrange
        var dto = new UserChangePassword
        {
            OldPassword = "old_password",
            NewPassword = "new_password"
        };

        _authServiceMock.Setup(x => x.ChangePasswordAsync(_userId, dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.ChangePassword(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();

        _authServiceMock.Verify(x => x.ChangePasswordAsync(_userId, dto), Times.Once);
    }
}