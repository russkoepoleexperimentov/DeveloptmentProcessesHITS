using Application.Services.Interfaces;
using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.Common;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace Tests.Controllers;

public class CommentControllerTests
{
    private readonly Mock<ICommentService> _commentServiceMock;
    private readonly CommentController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _postId;
    private readonly Guid _solutionId;
    private readonly Guid _commentId;

    public CommentControllerTests()
    {
        _commentServiceMock = new Mock<ICommentService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _postId = Guid.NewGuid();
        _solutionId = Guid.NewGuid();
        _commentId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) 
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new CommentController(_commentServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task CreatePostComment_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new AddCommentRequestDto { Text = "Test comment" };
        var expectedResult = new IdRequestDto { Id = Guid.NewGuid() };

        _commentServiceMock.Setup(x => x.CreatePostCommentAsync(_userId, _postId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreatePostComment(_postId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.CreatePostCommentAsync(_userId, _postId, dto), Times.Once);
    }

    [Fact]
    public async Task CreateSolutionComment_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new AddCommentRequestDto { Text = "Solution comment" };
        var expectedResult = new IdRequestDto { Id = Guid.NewGuid() };

        _commentServiceMock.Setup(x => x.CreateSolutionCommentAsync(_userId, _solutionId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateSolutionComment(_solutionId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.CreateSolutionCommentAsync(_userId, _solutionId, dto), Times.Once);
    }

    [Fact]
    public async Task GetPostRootComment_ShouldReturnOkWithCommentDtoList()
    {
        // Arrange
        var expectedResult = new List<CommentDto>
        {
            new CommentDto { Id = Guid.NewGuid(), Text = "Comment 1" },
            new CommentDto { Id = Guid.NewGuid(), Text = "Comment 2" }
        };

        _commentServiceMock.Setup(x => x.GetPostRootCommentAsync(_userId, _postId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPostRootComment(_postId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<CommentDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.GetPostRootCommentAsync(_userId, _postId), Times.Once);
    }

    [Fact]
    public async Task GetSolutionRootComment_ShouldReturnOkWithCommentDtoList()
    {
        // Arrange
        var expectedResult = new List<CommentDto>
        {
            new CommentDto { Id = Guid.NewGuid(), Text = "Solution comment 1" }
        };

        _commentServiceMock.Setup(x => x.GetSolutionRootCommentAsync(_userId, _solutionId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSolutionRootComment(_solutionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<CommentDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.GetSolutionRootCommentAsync(_userId, _solutionId), Times.Once);
    }

    [Fact]
    public async Task GetCommentReplies_ShouldReturnOkWithCommentDtoList()
    {
        // Arrange
        var expectedResult = new List<CommentDto>
        {
            new CommentDto { Id = Guid.NewGuid(), Text = "Reply 1" },
            new CommentDto { Id = Guid.NewGuid(), Text = "Reply 2" }
        };

        _commentServiceMock.Setup(x => x.GetCommentRepliesAsync(_userId, _commentId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCommentReplies(_commentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<CommentDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.GetCommentRepliesAsync(_userId, _commentId), Times.Once);
    }

    [Fact]
    public async Task CreateCommentReplyComment_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new AddCommentRequestDto { Text = "Reply to comment" };
        var expectedResult = new IdRequestDto { Id = Guid.NewGuid() };

        _commentServiceMock.Setup(x => x.CreateCommentReplyAsync(_userId, _commentId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateCommentReplyComment(_commentId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.CreateCommentReplyAsync(_userId, _commentId, dto), Times.Once);
    }

    [Fact]
    public async Task EditComment_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new EditCommentRequestDto { Text = "Updated comment text" };
        var expectedResult = new IdRequestDto { Id = _commentId };

        _commentServiceMock.Setup(x => x.EditCommentAsync(_userId, _commentId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.EditComment(_commentId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.EditCommentAsync(_userId, _commentId, dto), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var expectedResult = new IdRequestDto { Id = _commentId };

        _commentServiceMock.Setup(x => x.DeleteCommentAsync(_userId, _commentId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.DeleteComment(_commentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _commentServiceMock.Verify(x => x.DeleteCommentAsync(_userId, _commentId), Times.Once);
    }

    [Fact]
    public async Task CreatePostComment_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var dto = new AddCommentRequestDto { Text = "Test comment" };
        var expectedException = new Exception("Service error");

        _commentServiceMock.Setup(x => x.CreatePostCommentAsync(_userId, _postId, dto))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.CreatePostComment(_postId, dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task GetPostRootComment_WhenNoComments_ReturnsEmptyList()
    {
        // Arrange
        var expectedResult = new List<CommentDto>();

        _commentServiceMock.Setup(x => x.GetPostRootCommentAsync(_userId, _postId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetPostRootComment(_postId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<List<CommentDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEmpty();
        response.Message.Should().BeNull();
    }
}