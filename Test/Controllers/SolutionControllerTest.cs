using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
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

public class SolutionControllerTests
{
    private readonly Mock<ISolutionService> _solutionServiceMock;
    private readonly SolutionController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _taskId;
    private readonly Guid _solutionId;

    public SolutionControllerTests()
    {
        _solutionServiceMock = new Mock<ISolutionService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _taskId = Guid.NewGuid();
        _solutionId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new SolutionController(_solutionServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task SubmitSolution_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new SubmitSolutionRequestDto
        {
            Text = "My solution",
            Files = new List<Guid>()
        };

        var expectedResult = new IdRequestDto { Id = _solutionId };

        _solutionServiceMock
            .Setup(x => x.SubmitSolutionAsync(_userId, _taskId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.SubmitSolution(_taskId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _solutionServiceMock.Verify(x => x.SubmitSolutionAsync(_userId, _taskId, dto), Times.Once);
    }

    [Fact]
    public async Task DeleteSolution_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var expectedResult = new IdRequestDto { Id = _taskId };

        _solutionServiceMock
            .Setup(x => x.DeleteSolutionAsync(_userId, _taskId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.DeleteSolution(_taskId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _solutionServiceMock.Verify(x => x.DeleteSolutionAsync(_userId, _taskId), Times.Once);
    }

    [Fact]
    public async Task GetSolution_ShouldReturnOkWithStudentSolutionDetailsDto()
    {
        // Arrange
        var expectedResult = new StudentSolutionDetailsDto
        {
            Id = _solutionId,
            Text = "My solution",
            Status = SolutionStatus.Pending,
            Files = new List<Application.DTOs.Post.FileDto>(), 
            UpdatedDate = DateTime.UtcNow
        };

        _solutionServiceMock
            .Setup(x => x.GetSolutionByIdAsync(_userId, _taskId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSolution(_taskId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<StudentSolutionDetailsDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _solutionServiceMock.Verify(x => x.GetSolutionByIdAsync(_userId, _taskId), Times.Once);
    }

    [Fact]
    public async Task GetSolutionList_ShouldReturnOkWithSolutionListDto()
    {
        // Arrange
        int skip = 0;
        int take = 20;
        SolutionStatus? status = null;
        Guid? studentId = null;

        var expectedResult = new SolutionListDto
        {
            Records = new List<SolutionListItemDto>(),
            TotalRecords = 0
        };

        _solutionServiceMock
            .Setup(x => x.GetSolutionListAsync(_userId, _taskId, skip, take, status, studentId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSolutionList(_taskId, skip, take, status, studentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SolutionListDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _solutionServiceMock.Verify(x => x.GetSolutionListAsync(_userId, _taskId, skip, take, status, studentId), Times.Once);
    }

    [Fact]
    public async Task ReviewSolution_ShouldReturnOkWithIdRequestDto()
    {
        // Arrange
        var dto = new UpdateSolutionRequestDto
        {
            Score = 8,
            Status = SolutionStatus.Checked
        };

        var expectedResult = new IdRequestDto { Id = _solutionId };

        _solutionServiceMock
            .Setup(x => x.MarkSolutionAsync(_userId, _solutionId, dto))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ReviewSolution(_solutionId, dto);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<IdRequestDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _solutionServiceMock.Verify(x => x.MarkSolutionAsync(_userId, _solutionId, dto), Times.Once);
    }

    [Fact]
    public async Task SubmitSolution_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var dto = new SubmitSolutionRequestDto
        {
            Text = "My solution",
            Files = new List<Guid>()
        };

        var expectedException = new Exception("Service error");

        _solutionServiceMock
            .Setup(x => x.SubmitSolutionAsync(_userId, _taskId, dto))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.SubmitSolution(_taskId, dto);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task GetSolutionList_WhenNoSolutions_ReturnsEmptyList()
    {
        // Arrange
        int skip = 0;
        int take = 20;
        SolutionStatus? status = null;
        Guid? studentId = null;

        var expectedResult = new SolutionListDto
        {
            Records = new List<SolutionListItemDto>(),
            TotalRecords = 0
        };

        _solutionServiceMock
            .Setup(x => x.GetSolutionListAsync(_userId, _taskId, skip, take, status, studentId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetSolutionList(_taskId, skip, take, status, studentId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<SolutionListDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Data.Records.Should().BeEmpty();
        response.Data.TotalRecords.Should().Be(0);
        response.Message.Should().BeNull();
    }

    [Fact]
    public void AllMethods_ShouldHaveAuthorizeAttribute()
    {
        // SubmitSolution
        var submitMethod = typeof(SolutionController).GetMethod(nameof(SolutionController.SubmitSolution));
        var submitAttributes = submitMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        submitAttributes.Should().NotBeNullOrEmpty();

        // DeleteSolution
        var deleteMethod = typeof(SolutionController).GetMethod(nameof(SolutionController.DeleteSolution));
        var deleteAttributes = deleteMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        deleteAttributes.Should().NotBeNullOrEmpty();

        // GetSolution
        var getMethod = typeof(SolutionController).GetMethod(nameof(SolutionController.GetSolution));
        var getAttributes = getMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        getAttributes.Should().NotBeNullOrEmpty();

        // GetSolutionList
        var getListMethod = typeof(SolutionController).GetMethod(nameof(SolutionController.GetSolutionList));
        var getListAttributes = getListMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        getListAttributes.Should().NotBeNullOrEmpty();

        // ReviewSolution
        var reviewMethod = typeof(SolutionController).GetMethod(nameof(SolutionController.ReviewSolution));
        var reviewAttributes = reviewMethod?.GetCustomAttributes(typeof(AuthorizeAttribute), false);
        reviewAttributes.Should().NotBeNullOrEmpty();
    }
}