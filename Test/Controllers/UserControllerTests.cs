using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.User;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Authorization;

namespace Tests.Controllers;

public class UserControllerTests
{
    private readonly Mock<IUserService> _userServiceMock;
    private readonly UserController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _targetUserId;

    public UserControllerTests()
    {
        _userServiceMock = new Mock<IUserService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _targetUserId = Guid.NewGuid();

        // Создаем claims с userId
        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new UserController(_userServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task GetById_ShouldReturnOkWithUserDto()
    {
        // Arrange - из тестов сервиса UserDto имеет Id и Email/Credentials
        var expectedResult = new UserDto
        {
            Id = _targetUserId,
            Email = "test@test.com",
            Credentials = "testuser"
            // Name отсутствует в DTO
        };

        _userServiceMock
            .Setup(x => x.GetByIdAsync(_targetUserId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetById(_targetUserId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetByIdAsync(_targetUserId), Times.Once);
    }

    [Fact]
    public async Task UpdateSelf_ShouldReturnOkWithObject()
    {
        // Arrange - из тестов сервиса UserUpdateDto может быть пустым
        var dto = new UserUpdateDto
        {
            // Свойства зависят от реализации
        };

        _userServiceMock
            .Setup(x => x.UpdateAsync(_userId, dto))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.UpdateSelf(dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.UpdateAsync(_userId, dto), Times.Once);
    }

    [Fact]
    public async Task GetMe_ShouldReturnOkWithUserDto()
    {
        // Arrange
        var expectedResult = new UserDto
        {
            Id = _userId,
            Email = "current@test.com",
            Credentials = "current"
        };

        _userServiceMock
            .Setup(x => x.GetByIdAsync(_userId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMe();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetByIdAsync(_userId), Times.Once);
    }

    [Fact]
    public async Task SearchUsers_ShouldReturnOkWithListOfUserDto()
    {
        // Arrange
        string query = "test";
        var expectedResult = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "test1@test.com",
                Credentials = "test1"
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "test2@test.com",
                Credentials = "test2"
            }
        };

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync(query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchUsers(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<UserDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Data.Should().HaveCount(2);
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetAllUsersAsync(query), Times.Once);
    }

    [Fact]
    public async Task SearchUsers_WithNullQuery_ShouldReturnAllUsers()
    {
        // Arrange
        string? query = null;
        var expectedResult = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                Credentials = "user1"
            },
            new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com",
                Credentials = "user2"
            }
        };

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync(query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchUsers(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<UserDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Data.Should().HaveCount(2);
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetAllUsersAsync(query), Times.Once);
    }

    [Fact]
    public async Task SearchUsers_WithEmptyQuery_ShouldReturnAllUsers()
    {
        // Arrange
        string query = "";
        var expectedResult = new List<UserDto>
        {
            new UserDto
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                Credentials = "user1"
            }
        };

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync(query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchUsers(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<UserDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetAllUsersAsync(query), Times.Once);
    }

    [Fact]
    public async Task SearchUsers_WhenNoUsers_ReturnsEmptyList()
    {
        // Arrange
        string query = "nonexistent";
        var expectedResult = new List<UserDto>();

        _userServiceMock
            .Setup(x => x.GetAllUsersAsync(query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SearchUsers(query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<UserDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEmpty();
        response.Message.Should().BeNull();

        _userServiceMock.Verify(x => x.GetAllUsersAsync(query), Times.Once);
    }

    [Fact]
    public async Task GetById_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var expectedException = new Exception("Service error");

        _userServiceMock
            .Setup(x => x.GetByIdAsync(_targetUserId))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.GetById(_targetUserId);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task UpdateSelf_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var dto = new UserUpdateDto();
        var expectedException = new Exception("Service error");

        _userServiceMock
            .Setup(x => x.UpdateAsync(_userId, dto))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.UpdateSelf(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public void AllMethods_ShouldHaveAuthorizeAttribute()
    {
        // GetById
        var getByIdMethod = typeof(UserController).GetMethod(nameof(UserController.GetById));
        var getByIdAttributes = getByIdMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        getByIdAttributes.Should().NotBeNullOrEmpty();

        // UpdateSelf
        var updateMethod = typeof(UserController).GetMethod(nameof(UserController.UpdateSelf));
        var updateAttributes = updateMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        updateAttributes.Should().NotBeNullOrEmpty();

        // GetMe
        var getMeMethod = typeof(UserController).GetMethod(nameof(UserController.GetMe));
        var getMeAttributes = getMeMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        getMeAttributes.Should().NotBeNullOrEmpty();

        // SearchUsers
        var searchMethod = typeof(UserController).GetMethod(nameof(UserController.SearchUsers));
        var searchAttributes = searchMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        searchAttributes.Should().NotBeNullOrEmpty();
    }
}