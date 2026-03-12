using Application.Services.Implementations;
using GoogleClass.DTOs;
using GoogleClass.Models;

namespace Tests;

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

public class SolutionServiceTests
{
    private readonly GcDbContext _context;
    private readonly Mock<UserManager<User>> _userManagerMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly Mock<IValidator<SubmitSolutionRequestDto>> _submitValidator;
    private readonly Mock<IValidator<UpdateSolutionRequestDto>> _updateValidator;

    private readonly SolutionService _service;

    public SolutionServiceTests()
    {
        var options = new DbContextOptionsBuilder<GcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new GcDbContext(options);

        var store = new Mock<IUserStore<User>>();

        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        _mapperMock = new Mock<IMapper>();

        _submitValidator = new Mock<IValidator<SubmitSolutionRequestDto>>();
        _updateValidator = new Mock<IValidator<UpdateSolutionRequestDto>>();

        _submitValidator
            .Setup(v => v.ValidateAsync(It.IsAny<SubmitSolutionRequestDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _updateValidator
            .Setup(v => v.ValidateAsync(It.IsAny<UpdateSolutionRequestDto>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());

        _service = new(_context, _userManagerMock.Object, _mapperMock.Object, _submitValidator.Object, _updateValidator.Object);
    }

    [Fact]
    public async Task SubmitSolutionAsync_ShouldCreateSolution()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var assignment = new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            MaxScore = 10
        };

        _context.Assignments.Add(assignment);

        _context.CourseRoles.Add(new CourseRole
        {
            CourseId = courseId,
            UserId = userId,
            RoleType = UserRoleType.Student
        });

        await _context.SaveChangesAsync();

        var dto = new SubmitSolutionRequestDto
        {
            Text = "My solution"
        };

        var result = await _service.SubmitSolutionAsync(userId, taskId, dto);

        result.Id.Should().NotBeEmpty();

        var solution = await _context.Solutions.FirstAsync();

        solution.Text.Should().Be("My solution");
        solution.Status.Should().Be(SolutionStatus.Pending);
    }

    [Fact]
    public async Task SubmitSolutionAsync_ShouldUpdateExistingSolution()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var assignment = new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            MaxScore = 10
        };

        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Text = "Old text",
            Status = SolutionStatus.Checked
        };

        _context.Assignments.Add(assignment);
        _context.Solutions.Add(solution);

        _context.CourseRoles.Add(new CourseRole
        {
            CourseId = courseId,
            UserId = userId,
            RoleType = UserRoleType.Student
        });

        await _context.SaveChangesAsync();

        var dto = new SubmitSolutionRequestDto
        {
            Text = "Updated text"
        };

        await _service.SubmitSolutionAsync(userId, taskId, dto);

        var updated = await _context.Solutions.FirstAsync();

        updated.Text.Should().Be("Updated text");
        updated.Status.Should().Be(SolutionStatus.Pending);
    }

    [Fact]
    public async Task DeleteSolutionAsync_ShouldDeleteSolution()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Text = "Solution"
        };

        _context.Solutions.Add(solution);
        await _context.SaveChangesAsync();

        await _service.DeleteSolutionAsync(userId, taskId);

        _context.Solutions.Count().Should().Be(0);
    }

    [Fact]
    public async Task GetSolutionByIdAsync_ShouldReturnSolution()
    {
        var userId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Text = "Solution text",
            Status = SolutionStatus.Pending,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Solutions.Add(solution);
        await _context.SaveChangesAsync();

        var result = await _service.GetSolutionByIdAsync(userId, taskId);

        result.Text.Should().Be("Solution text");
        result.Status.Should().Be(SolutionStatus.Pending);
    }

    [Fact]
    public async Task GetSolutionListAsync_ShouldReturnSolutions()
    {
        var teacherId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var course = new Course
        {
            Id = courseId,
            Title = "Test course",
            InviteCode = "12345"
        };

        var assignment = new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            Course = course,
            MaxScore = 10
        };

        var student = new User
        {
            Id = studentId,
            Credentials = "student"
        };

        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = studentId,
            User = student,
            Text = "Solution text",
            Status = SolutionStatus.Pending,
            UpdatedDate = DateTime.UtcNow
        };

        _context.Courses.Add(course);
        _context.Assignments.Add(assignment);
        _context.Users.Add(student);
        _context.Solutions.Add(solution);

        _context.CourseRoles.Add(new CourseRole
        {
            CourseId = courseId,
            UserId = teacherId,
            RoleType = UserRoleType.Teacher
        });

        await _context.SaveChangesAsync();

        var result = await _service.GetSolutionListAsync(
            teacherId,
            taskId,
            0,
            10,
            null,
            null);

        result.TotalRecords.Should().Be(1);
        result.Records.Should().HaveCount(1);
    }

    [Fact]
    public async Task MarkSolutionAsync_ShouldUpdateScoreAndStatus()
    {
        var teacherId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();

        var assignment = new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            MaxScore = 10
        };

        var solution = new Solution
        {
            Id = Guid.NewGuid(),
            Text = "newsolution",
            TaskId = taskId,
            Task = assignment,
            Status = SolutionStatus.Pending
        };

        _context.Assignments.Add(assignment);
        _context.Solutions.Add(solution);

        _context.CourseRoles.Add(new CourseRole
        {
            CourseId = courseId,
            UserId = teacherId,
            RoleType = UserRoleType.Teacher
        });

        await _context.SaveChangesAsync();

        var dto = new UpdateSolutionRequestDto
        {
            Score = 8,
            Status = SolutionStatus.Checked
        };

        await _service.MarkSolutionAsync(teacherId, solution.Id, dto);

        var updated = await _context.Solutions.FirstAsync();

        updated.Score.Should().Be(8);
        updated.Status.Should().Be(SolutionStatus.Checked);
    }
}