using Application.Services.Interfaces;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.DTOs.Course.Application.DTOs.User;
using GoogleClass.Models;
using GoogleClassroom.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace Tests.Controllers;

public class CourseControllerTests
{
    private readonly Mock<ICourseService> _courseServiceMock;
    private readonly CourseController _controller;
    private readonly Mock<HttpContext> _httpContextMock;
    private readonly Guid _userId;
    private readonly Guid _courseId;
    private readonly Guid _targetUserId;

    public CourseControllerTests()
    {
        _courseServiceMock = new Mock<ICourseService>();
        _httpContextMock = new Mock<HttpContext>();
        _userId = Guid.NewGuid();
        _courseId = Guid.NewGuid();
        _targetUserId = Guid.NewGuid();

        var claims = new List<Claim>
        {
            new Claim("userId", _userId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _httpContextMock.Setup(x => x.User).Returns(principal);

        _controller = new CourseController(_courseServiceMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = _httpContextMock.Object
            }
        };
    }

    [Fact]
    public async Task CreateCourse_ShouldReturnOkWithCreateUpdateCourseResponseDto()
    {
        // Arrange
        var request = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
        };

        var expectedResult = new CreateUpdateCourseResponseDto
        {
            Id = _courseId,
            Title = "Test Course"
        };

        _courseServiceMock.Setup(x => x.CreateCourseAsync(_userId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.CreateCourse(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<CreateUpdateCourseResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.CreateCourseAsync(_userId, request), Times.Once);
    }

    [Fact]
    public async Task UpdateCourse_ShouldReturnOkWithCreateUpdateCourseResponseDto()
    {
        // Arrange
        var request = new CreateUpdateCourseRequestDto
        {
            Title = "Updated Course"
        };

        var expectedResult = new CreateUpdateCourseResponseDto
        {
            Id = _courseId,
            Title = "Updated Course"
        };

        _courseServiceMock.Setup(x => x.UpdateCourseAsync(_userId, _courseId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.UpdateCourse(_courseId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<CreateUpdateCourseResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.UpdateCourseAsync(_userId, _courseId, request), Times.Once);
    }

    [Fact]
    public async Task GetCourseDetails_ShouldReturnOkWithCourseDetailsDto()
    {
        // Arrange
        var expectedResult = new CourseDetailsDto
        {
            Id = _courseId,
            Title = "Test Course",
            AuthorId = _userId,
            InviteCode = "CODE",
            Role = UserRoleType.Teacher
        };

        _courseServiceMock.Setup(x => x.GetCourseDetailsAsync(_userId, _courseId))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetCourseDetails(_courseId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<CourseDetailsDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.GetCourseDetailsAsync(_userId, _courseId), Times.Once);
    }

    [Fact]
    public async Task GetMembers_ShouldReturnOkWithPagedResponseOfCourseMemberDto()
    {
        // Arrange
        int skip = 0;
        int take = 10;
        string? query = null;

        var expectedResult = new PagedResponse<CourseMemberDto>
        {
            Records = new List<CourseMemberDto>
            {
                new CourseMemberDto(),
                new CourseMemberDto()
            },
            TotalRecords = 2
        };

        _courseServiceMock.Setup(x => x.GetMembersAsync(_userId, _courseId, skip, take, query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMembers(_courseId, skip, take, query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<CourseMemberDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.GetMembersAsync(_userId, _courseId, skip, take, query), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_ShouldReturnOkWithChangeRoleResponseDto()
    {
        // Arrange
        var request = new ChangeRoleRequestDto { Role = UserRoleType.Teacher };
        var expectedResult = new ChangeRoleResponseDto
        {
            Id = _targetUserId,
            Role = UserRoleType.Teacher
        };

        _courseServiceMock.Setup(x => x.ChangeRoleAsync(_userId, _courseId, _targetUserId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.ChangeRole(_courseId, _targetUserId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<ChangeRoleResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.ChangeRoleAsync(_userId, _courseId, _targetUserId, request), Times.Once);
    }

    [Fact]
    public async Task RemoveMember_ShouldReturnOkWithObject()
    {
        // Arrange
        _courseServiceMock.Setup(x => x.RemoveMemberAsync(_userId, _courseId, _targetUserId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.RemoveMember(_courseId, _targetUserId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.RemoveMemberAsync(_userId, _courseId, _targetUserId), Times.Once);
    }

    [Fact]
    public async Task LeaveCourse_ShouldReturnOkWithObject()
    {
        // Arrange
        _courseServiceMock.Setup(x => x.RemoveMemberAsync(_userId, _courseId, _userId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.LeaveCourse(_courseId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<object>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.RemoveMemberAsync(_userId, _courseId, _userId), Times.Once);
    }

    [Fact]
    public async Task JoinCourse_ShouldReturnOkWithJoinCourseResponseDto()
    {
        // Arrange
        var request = new JoinCourseRequestDto { InviteCode = "SECRET123" };
        var expectedResult = new JoinCourseResponseDto
        {
            Id = _courseId,
            Title = "Test Course",
            Role = UserRoleType.Student
        };

        _courseServiceMock.Setup(x => x.JoinCourseAsync(_userId, request))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.JoinCourse(request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<JoinCourseResponseDto>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.JoinCourseAsync(_userId, request), Times.Once);
    }

    [Fact]
    public async Task GetMyCourses_ShouldReturnOkWithPagedResponseOfUserCourseDto()
    {
        // Arrange
        int skip = 0;
        int take = 20;

        var expectedResult = new PagedResponse<UserCourseDto>
        {
            Records = new List<UserCourseDto>
            {
                new UserCourseDto
                {
                    Id = _courseId,
                    Title = "Course 1",
                    Role = UserRoleType.Teacher
                },
                new UserCourseDto
                {
                    Id = Guid.NewGuid(),
                    Title = "Course 2",
                    Role = UserRoleType.Student
                }
            },
            TotalRecords = 2
        };

        _courseServiceMock
            .Setup(x => x.GetUserCoursesAsync(_userId, skip, take))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMyCourses(skip, take);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<UserCourseDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.GetUserCoursesAsync(_userId, skip, take), Times.Once);
    }

    [Fact]
    public async Task GetMembers_WithQuery_ShouldReturnFilteredMembers()
    {
        // Arrange
        int skip = 0;
        int take = 10;
        string query = "test";

        var expectedResult = new PagedResponse<CourseMemberDto>
        {
            Records = new List<CourseMemberDto>
            {
                new CourseMemberDto()
            },
            TotalRecords = 1
        };

        _courseServiceMock.Setup(x => x.GetMembersAsync(_userId, _courseId, skip, take, query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMembers(_courseId, skip, take, query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<CourseMemberDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().BeEquivalentTo(expectedResult);
        response.Message.Should().BeNull();

        _courseServiceMock.Verify(x => x.GetMembersAsync(_userId, _courseId, skip, take, query), Times.Once);
    }

    [Fact]
    public async Task CreateCourse_WhenServiceThrows_ShouldPropagateException()
    {
        // Arrange
        var request = new CreateUpdateCourseRequestDto
        {
            Title = "Test Course"
        };

        var expectedException = new Exception("Service error");

        _courseServiceMock.Setup(x => x.CreateCourseAsync(_userId, request))
            .ThrowsAsync(expectedException);

        // Act
        Func<Task> act = async () => await _controller.CreateCourse(request);

        // Assert
        await act.Should().ThrowAsync<Exception>().WithMessage("Service error");
    }

    [Fact]
    public async Task GetMembers_WhenNoMembers_ReturnsEmptyList()
    {
        // Arrange
        int skip = 0;
        int take = 10;
        string? query = null;

        var expectedResult = new PagedResponse<CourseMemberDto>
        {
            Records = new List<CourseMemberDto>(),
            TotalRecords = 0
        };

        _courseServiceMock.Setup(x => x.GetMembersAsync(_userId, _courseId, skip, take, query))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _controller.GetMembers(_courseId, skip, take, query);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<ApiResponse<PagedResponse<CourseMemberDto>>>().Subject;

        response.Type.Should().Be(ApiResponseType.Success);
        response.Data.Should().NotBeNull();
        response.Data.Records.Should().BeEmpty();
        response.Data.TotalRecords.Should().Be(0);
        response.Message.Should().BeNull();
    }
}