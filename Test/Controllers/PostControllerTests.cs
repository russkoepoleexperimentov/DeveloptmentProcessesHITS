using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course; 
using GoogleClass.DTOs.Post;
using GoogleClass.Models;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Authorization;

namespace Tests.Controllers;

public class PostControllerTests
{
    private readonly Mock<IPostService> _postServiceMock;
    private readonly PostController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _courseId;
    private readonly Guid _postId;

    public PostControllerTests()
    {
        _postServiceMock = new Mock<IPostService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _courseId = Guid.NewGuid();
        _postId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new PostController(_postServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task CreatePost_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Test Post",
            Text = "Test Content"
        };

        var expectedResult = new IdRequestDto { Id = _postId };

        _postServiceMock
            .Setup(x => x.CreatePostAsync(_userId, _courseId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreatePost(_courseId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.CreatePostAsync(_userId, _courseId, dto), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WithTaskType_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new CreateUpdatePostDto
        {
            Type = PostType.TASK,
            Title = "Test Task",
            Text = "Solve this",
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxScore = 10,
            TaskType = TaskType.Mandatory,
            SolvableAfterDeadline = true,
            Files = new List<Guid>()
        };

        var expectedResult = new IdRequestDto { Id = _postId };

        _postServiceMock
            .Setup(x => x.CreatePostAsync(_userId, _courseId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreatePost(_courseId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.CreatePostAsync(_userId, _courseId, dto), Times.Once);
    }

    [Fact]
    public async Task GetPost_ShouldReturnOkWithPostDetailsDto()
    {
        // Arrange
        var expectedResult = new PostDetailsDto
        {
            Id = _postId,
            Type = PostType.POST,
            Title = "Test Post",
            Text = "Test Content",
            Files = new List<FileDto>(),
            UserSolution = null
        };

        _postServiceMock
            .Setup(x => x.GetPostAsync(_userId, _postId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPost(_postId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PostDetailsDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.GetPostAsync(_userId, _postId), Times.Once);
    }

    [Fact]
    public async Task GetPost_WithTaskType_ShouldReturnOkWithPostDetailsDto()
    {
        // Arrange
        var expectedResult = new PostDetailsDto
        {
            Id = _postId,
            Type = PostType.TASK,
            Title = "Test Task",
            Text = "Solve this",
            Deadline = DateTime.UtcNow.AddDays(7),
            MaxScore = 10,
            TaskType = TaskType.Mandatory,
            SolvableAfterDeadline = true,
            Files = new List<FileDto>(),
            UserSolution = null
        };

        _postServiceMock
            .Setup(x => x.GetPostAsync(_userId, _postId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPost(_postId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PostDetailsDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.GetPostAsync(_userId, _postId), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Updated Post",
            Text = "Updated Content"
        };

        var expectedResult = new IdRequestDto { Id = _postId };

        _postServiceMock
            .Setup(x => x.UpdatePostAsync(_userId, _postId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UpdatePost(_postId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.UpdatePostAsync(_userId, _postId, dto), Times.Once);
    }

    [Fact]
    public async Task UpdatePost_WithTaskType_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new CreateUpdatePostDto
        {
            Type = PostType.TASK,
            Title = "Updated Task",
            Text = "Updated Content",
            Deadline = DateTime.UtcNow.AddDays(14),
            MaxScore = 20,
            TaskType = TaskType.Optional,
            SolvableAfterDeadline = false,
            Files = new List<Guid>()
        };

        var expectedResult = new IdRequestDto { Id = _postId };

        _postServiceMock
            .Setup(x => x.UpdatePostAsync(_userId, _postId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UpdatePost(_postId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.UpdatePostAsync(_userId, _postId, dto), Times.Once);
    }

    [Fact]
    public async Task DeletePost_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var expectedResult = new IdRequestDto { Id = _postId };

        _postServiceMock
            .Setup(x => x.DeletePostAsync(_userId, _postId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.DeletePost(_postId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.DeletePostAsync(_userId, _postId), Times.Once);
    }

    [Fact]
    public async Task GetCourseFeed_ShouldReturnOkWithFeedResponseDto()
    {
        // Arrange
        int skip = 0;
        int take = 20;

        var expectedResult = new FeedResponseDto
        {
            Records = new List<CourseFeedItemDto>(), 
            TotalRecords = 2
        };

        _postServiceMock
            .Setup(x => x.GetCourseFeedAsync(_userId, _courseId, skip, take))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCourseFeed(_courseId, skip, take);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<FeedResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.GetCourseFeedAsync(_userId, _courseId, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetCourseFeed_WithSkipTake_ShouldReturnPagedFeed()
    {
        // Arrange
        int skip = 2;
        int take = 3;

        var expectedResult = new FeedResponseDto
        {
            Records = new List<CourseFeedItemDto>(), 
            TotalRecords = 10
        };

        _postServiceMock
            .Setup(x => x.GetCourseFeedAsync(_userId, _courseId, skip, take))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCourseFeed(_courseId, skip, take);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<FeedResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _postServiceMock.Verify(x => x.GetCourseFeedAsync(_userId, _courseId, skip, take), Times.Once);
    }

    [Fact]
    public async Task CreatePost_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var dto = new CreateUpdatePostDto
        {
            Type = PostType.POST,
            Title = "Test Post",
            Text = "Test Content"
        };

        var expectedException = new Exception("Service error");

        _postServiceMock
            .Setup(x => x.CreatePostAsync(_userId, _courseId, dto))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.CreatePost(_courseId, dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task GetCourseFeed_WhenNoPosts_ReturnsEmptyList()
    {
        // Arrange
        int skip = 0;
        int take = 20;

        var expectedResult = new FeedResponseDto
        {
            Records = new List<CourseFeedItemDto>(), 
            TotalRecords = 0
        };

        _postServiceMock
            .Setup(x => x.GetCourseFeedAsync(_userId, _courseId, skip, take))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCourseFeed(_courseId, skip, take);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<FeedResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Data.Records.Should().BeEmpty();
        response.Data.TotalRecords.Should().Be(0);
        response.Message.Should().BeNull();
    }

    [Fact]
    public void AllMethods_ShouldHaveAuthorizeAttribute()
    {
        // Act & Assert for CreatePost
        var createMethod = typeof(PostController).GetMethod(nameof(PostController.CreatePost));
        var createAttributes = createMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        createAttributes.Should().NotBeNullOrEmpty();

        // Act & Assert for GetPost
        var getMethod = typeof(PostController).GetMethod(nameof(PostController.GetPost));
        var getAttributes = getMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        getAttributes.Should().NotBeNullOrEmpty();

        // Act & Assert for UpdatePost
        var updateMethod = typeof(PostController).GetMethod(nameof(PostController.UpdatePost));
        var updateAttributes = updateMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        updateAttributes.Should().NotBeNullOrEmpty();

        // Act & Assert for DeletePost
        var deleteMethod = typeof(PostController).GetMethod(nameof(PostController.DeletePost));
        var deleteAttributes = deleteMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        deleteAttributes.Should().NotBeNullOrEmpty();

        // Act & Assert for GetCourseFeed
        var feedMethod = typeof(PostController).GetMethod(nameof(PostController.GetCourseFeed));
        var feedAttributes = feedMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        feedAttributes.Should().NotBeNullOrEmpty();
    }
}