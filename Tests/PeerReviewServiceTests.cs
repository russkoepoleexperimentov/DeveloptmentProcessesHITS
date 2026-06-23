using Application.DTOs.Grading;
using Application.DTOs.Grading.PeerReview;
using Application.Services.Implementations;
using Common.Exceptions;
using Domain.Models;
using Domain.Models.Criteria;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests;

public class PeerReviewServiceTests
{
    private readonly GcDbContext _context;
    private readonly Mock<IValidator<SubmitPeerReviewDto>> _validatorMock;
    private readonly PeerReviewService _service;

    public PeerReviewServiceTests()
    {
        var options = new DbContextOptionsBuilder<GcDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new GcDbContext(options);

        _validatorMock = new Mock<IValidator<SubmitPeerReviewDto>>();
        _validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<SubmitPeerReviewDto>(), default))
            .ReturnsAsync(new ValidationResult());

        _service = new PeerReviewService(_context, _validatorMock.Object);
    }

    // ──────────────────────────────────────────────────────────────
    //  Helpers
    // ──────────────────────────────────────────────────────────────

    private async Task<(Guid courseId, Guid taskId, Guid userId)> SeedIndividualP2PAsync(int minReviews = 1)
    {
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Assignments.Add(new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            GradingMode = GradingMode.PeerToPeer,
            MinPeerReviewsRequired = minReviews,
            MaxScore = 100,
            Title = "P2P Task"
        });

        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(),
            CourseId = courseId,
            UserId = userId,
            RoleType = UserRoleType.Student
        });

        await _context.SaveChangesAsync();
        return (courseId, taskId, userId);
    }

    private async Task<Guid> SeedSolutionAsync(Guid taskId, Guid userId)
    {
        var solutionId = Guid.NewGuid();
        _context.Solutions.Add(new Solution
        {
            Id = solutionId,
            TaskId = taskId,
            UserId = userId,
            Text = "My solution"
        });
        await _context.SaveChangesAsync();
        return solutionId;
    }

    private async Task<(Guid courseId, Guid taskId, Guid team1Id, Guid userId1, Guid team2Id, Guid userId2)>
        SeedTeamP2PAsync()
    {
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId1 = Guid.NewGuid();
        var userId2 = Guid.NewGuid();
        var team1Id = Guid.NewGuid();
        var team2Id = Guid.NewGuid();

        _context.TeamAssignments.Add(new TeamAssignment
        {
            Id = taskId,
            CourseId = courseId,
            GradingMode = GradingMode.PeerToPeer,
            Title = "Team P2P Task",
            MaxScore = 100
        });

        _context.CourseRoles.AddRange(
            new CourseRole { Id = Guid.NewGuid(), CourseId = courseId, UserId = userId1, RoleType = UserRoleType.Student },
            new CourseRole { Id = Guid.NewGuid(), CourseId = courseId, UserId = userId2, RoleType = UserRoleType.Student }
        );

        _context.Teams.AddRange(
            new Team { Id = team1Id, CourseId = courseId, AssignmentId = taskId, Name = "Team 1" },
            new Team { Id = team2Id, CourseId = courseId, AssignmentId = taskId, Name = "Team 2" }
        );

        _context.TeamMembers.AddRange(
            new TeamMember { Id = Guid.NewGuid(), TeamId = team1Id, UserId = userId1 },
            new TeamMember { Id = Guid.NewGuid(), TeamId = team2Id, UserId = userId2 }
        );

        await _context.SaveChangesAsync();
        return (courseId, taskId, team1Id, userId1, team2Id, userId2);
    }

    // ──────────────────────────────────────────────────────────────
    //  GetNextAssignmentAsync — individual P2P
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetNextAssignment_ShouldThrowNotFound_WhenTaskDoesNotExist()
    {
        var act = () => _service.GetNextAssignmentAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetNextAssignment_ShouldThrowBadRequest_WhenTaskIsNotP2P()
    {
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Assignments.Add(new Assignment
        {
            Id = taskId,
            CourseId = courseId,
            GradingMode = GradingMode.TeacherReview,
            MaxScore = 100,
            Title = "Teacher task"
        });
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = userId, RoleType = UserRoleType.Student
        });
        await _context.SaveChangesAsync();

        var act = () => _service.GetNextAssignmentAsync(userId, taskId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*not enabled*");
    }

    [Fact]
    public async Task GetNextAssignment_ShouldThrowForbidden_WhenUserIsNotCourseStudent()
    {
        var (_, taskId, _) = await SeedIndividualP2PAsync();
        var outsiderId = Guid.NewGuid();

        var act = () => _service.GetNextAssignmentAsync(outsiderId, taskId);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task GetNextAssignment_ShouldThrowForbidden_WhenUserIsTeacher()
    {
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var teacherId = Guid.NewGuid();

        _context.Assignments.Add(new Assignment
        {
            Id = taskId, CourseId = courseId, GradingMode = GradingMode.PeerToPeer,
            MaxScore = 100, Title = "P2P Task"
        });
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = teacherId, RoleType = UserRoleType.Teacher
        });
        await _context.SaveChangesAsync();

        var act = () => _service.GetNextAssignmentAsync(teacherId, taskId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*Only students*");
    }

    [Fact]
    public async Task GetNextAssignment_ShouldThrowBadRequest_WhenUserHasNoOwnSolution()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();

        var act = () => _service.GetNextAssignmentAsync(userId, taskId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Submit your own solution*");
    }

    [Fact]
    public async Task GetNextAssignment_ShouldReturnNull_WhenNoCandidateSolutionsAvailable()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        await SeedSolutionAsync(taskId, userId);

        var result = await _service.GetNextAssignmentAsync(userId, taskId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextAssignment_ShouldCreateAndReturnReview_WhenCandidateExists()
    {
        var (courseId, taskId, userId) = await SeedIndividualP2PAsync();
        await SeedSolutionAsync(taskId, userId);

        var otherUserId = Guid.NewGuid();
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = otherUserId, RoleType = UserRoleType.Student
        });
        var otherSolutionId = await SeedSolutionAsync(taskId, otherUserId);

        var result = await _service.GetNextAssignmentAsync(userId, taskId);

        result.Should().NotBeNull();
        result!.TaskId.Should().Be(taskId);

        var review = await _context.PeerReviews.FirstAsync();
        review.ReviewerId.Should().Be(userId);
        review.SolutionId.Should().Be(otherSolutionId);
        review.Status.Should().Be(PeerReviewStatus.Assigned);
    }

    [Fact]
    public async Task GetNextAssignment_ShouldReturnExistingAssigned_WhenOneAlreadyExists()
    {
        var (courseId, taskId, userId) = await SeedIndividualP2PAsync();
        await SeedSolutionAsync(taskId, userId);

        var otherUserId = Guid.NewGuid();
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = otherUserId, RoleType = UserRoleType.Student
        });
        var otherSolutionId = await SeedSolutionAsync(taskId, otherUserId);

        var existingReviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = existingReviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetNextAssignmentAsync(userId, taskId);

        result.Should().NotBeNull();
        result!.ReviewId.Should().Be(existingReviewId);
        _context.PeerReviews.Count().Should().Be(1);
    }

    [Fact]
    public async Task GetNextAssignment_ShouldNotAssignAlreadyReviewedSolution()
    {
        var (courseId, taskId, userId) = await SeedIndividualP2PAsync();
        await SeedSolutionAsync(taskId, userId);

        var other1Id = Guid.NewGuid();
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = other1Id, RoleType = UserRoleType.Student
        });
        var sol1 = await SeedSolutionAsync(taskId, other1Id);

        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = sol1,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetNextAssignmentAsync(userId, taskId);

        result.Should().BeNull();
    }

    // ──────────────────────────────────────────────────────────────
    //  SubmitReviewAsync — individual P2P
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitReview_ShouldThrowNotFound_WhenReviewDoesNotExist()
    {
        var act = () => _service.SubmitReviewAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitReview_ShouldThrowForbidden_WhenReviewBelongsToAnotherUser()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());
        await SeedSolutionAsync(taskId, userId);

        var reviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = reviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var act = () => _service.SubmitReviewAsync(Guid.NewGuid(), reviewId, new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task SubmitReview_ShouldThrowBadRequest_WhenReviewAlreadyCompleted()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());
        await SeedSolutionAsync(taskId, userId);

        var reviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = reviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var act = () => _service.SubmitReviewAsync(userId, reviewId, new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*already completed*");
    }

    [Fact]
    public async Task SubmitReview_ShouldThrowBadRequest_WhenScoreExceedsMax()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());

        var criterionId = Guid.NewGuid();
        _context.WeightedCriteria.Add(new WeightedCriterion
        {
            Id = criterionId,
            PostId = taskId,
            MaxScore = 10f,
            Weight = 1f,
            Title = "Quality"
        });

        var reviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = reviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var dto = new SubmitPeerReviewDto
        {
            Evaluation = new EvaluationDto
            {
                WeightedValues = new List<WeightedValueDto>
                {
                    new() { CriterionId = criterionId, Score = 999f }
                }
            }
        };

        var act = () => _service.SubmitReviewAsync(userId, reviewId, dto);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*exceeds max*");
    }

    [Fact]
    public async Task SubmitReview_ShouldThrowBadRequest_WhenUnknownCriterionProvided()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());

        var reviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = reviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var dto = new SubmitPeerReviewDto
        {
            Evaluation = new EvaluationDto
            {
                WeightedValues = new List<WeightedValueDto>
                {
                    new() { CriterionId = Guid.NewGuid(), Score = 5f }
                }
            }
        };

        var act = () => _service.SubmitReviewAsync(userId, reviewId, dto);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Unknown or non-weighted criterion*");
    }

    [Fact]
    public async Task SubmitReview_ShouldMarkReviewCompleted_AndPersistValues()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());

        var criterionId = Guid.NewGuid();
        _context.WeightedCriteria.Add(new WeightedCriterion
        {
            Id = criterionId,
            PostId = taskId,
            MaxScore = 10f,
            Weight = 1f,
            Title = "Quality"
        });

        var reviewId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = reviewId,
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var dto = new SubmitPeerReviewDto
        {
            Evaluation = new EvaluationDto
            {
                WeightedValues = new List<WeightedValueDto>
                {
                    new() { CriterionId = criterionId, Score = 8f }
                }
            }
        };

        var progress = await _service.SubmitReviewAsync(userId, reviewId, dto);

        var review = await _context.PeerReviews.FirstAsync(r => r.Id == reviewId);
        review.Status.Should().Be(PeerReviewStatus.Completed);
        review.CompletedAt.Should().NotBeNull();

        var value = await _context.WeightedCriterionValues.FirstAsync();
        value.Score.Should().Be(8f);
        value.PeerReviewId.Should().Be(reviewId);
        value.IsSelfAssessment.Should().BeFalse();

        progress.Completed.Should().Be(1);
        progress.CanFinish.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    //  GetIndividualProgressAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIndividualProgress_ShouldThrowNotFound_WhenTaskNotFound()
    {
        var act = () => _service.GetIndividualProgressAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetIndividualProgress_ShouldThrowBadRequest_WhenNotP2P()
    {
        var courseId = Guid.NewGuid();
        var taskId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _context.Assignments.Add(new Assignment
        {
            Id = taskId, CourseId = courseId, GradingMode = GradingMode.TeacherReview,
            MaxScore = 100, Title = "Teacher task"
        });
        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(), CourseId = courseId, UserId = userId, RoleType = UserRoleType.Student
        });
        await _context.SaveChangesAsync();

        var act = () => _service.GetIndividualProgressAsync(userId, taskId);

        await act.Should().ThrowAsync<BadRequestException>();
    }

    [Fact]
    public async Task GetIndividualProgress_ShouldReturnCorrectCounts()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync(minReviews: 2);
        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());

        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var progress = await _service.GetIndividualProgressAsync(userId, taskId);

        progress.Required.Should().Be(2);
        progress.Completed.Should().Be(1);
        progress.CanFinish.Should().BeFalse();
        progress.IsCounted.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    //  FinishAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Finish_ShouldThrowBadRequest_WhenNoSolution()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync();

        var act = () => _service.FinishAsync(userId, taskId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*No attached solution*");
    }

    [Fact]
    public async Task Finish_ShouldThrowBadRequest_WhenNotEnoughReviewsCompleted()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync(minReviews: 2);
        await SeedSolutionAsync(taskId, userId);

        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var act = () => _service.FinishAsync(userId, taskId);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Minimum reviews not completed*");
    }

    [Fact]
    public async Task Finish_ShouldMarkSolutionAsCounted_WhenRequirementsMet()
    {
        var (_, taskId, userId) = await SeedIndividualP2PAsync(minReviews: 1);
        var solutionId = await SeedSolutionAsync(taskId, userId);

        var otherSolutionId = await SeedSolutionAsync(taskId, Guid.NewGuid());
        var now = DateTime.UtcNow;
        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId,
            SolutionId = otherSolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var progress = await _service.FinishAsync(userId, taskId);

        var solution = await _context.Solutions.FirstAsync(s => s.Id == solutionId);
        solution.PeerReviewCounted.Should().BeTrue();

        progress.IsCounted.Should().BeTrue();
        progress.CanFinish.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    //  GetAvailableTeamSolutionsAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableTeamSolutions_ShouldThrowNotFound_WhenTaskNotFound()
    {
        var act = () => _service.GetAvailableTeamSolutionsAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetAvailableTeamSolutions_ShouldThrowForbidden_WhenUserNotInTeam()
    {
        var (_, taskId, _, _, _, _) = await SeedTeamP2PAsync();
        var outsiderId = Guid.NewGuid();

        _context.CourseRoles.Add(new CourseRole
        {
            Id = Guid.NewGuid(),
            CourseId = (await _context.TeamAssignments.FirstAsync(t => t.Id == taskId)).CourseId,
            UserId = outsiderId,
            RoleType = UserRoleType.Student
        });
        await _context.SaveChangesAsync();

        var act = () => _service.GetAvailableTeamSolutionsAsync(outsiderId, taskId);

        await act.Should().ThrowAsync<ForbiddenException>()
            .WithMessage("*not in a team*");
    }

    [Fact]
    public async Task GetAvailableTeamSolutions_ShouldExcludeOwnTeamSolution()
    {
        var (_, taskId, team1Id, userId1, team2Id, userId2) = await SeedTeamP2PAsync();

        var now = DateTime.UtcNow;
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            TeamId = team1Id,
            SubmittedByUserId = userId1,
            Text = "Team 1 solution",
            SubmittedAt = now
        });
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            TeamId = team2Id,
            SubmittedByUserId = userId2,
            Text = "Team 2 solution",
            SubmittedAt = now
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetAvailableTeamSolutionsAsync(userId1, taskId);

        result.Records.Should().HaveCount(1);
        var record = result.Records.Single();
        record.TeamName.Should().Be("Team 2");
        record.AlreadyReviewed.Should().BeFalse();
    }

    [Fact]
    public async Task GetAvailableTeamSolutions_ShouldMarkAlreadyReviewed()
    {
        var (_, taskId, team1Id, userId1, team2Id, userId2) = await SeedTeamP2PAsync();

        var now = DateTime.UtcNow;
        var team2SolutionId = Guid.NewGuid();
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            TeamId = team1Id,
            SubmittedByUserId = userId1,
            Text = "Team 1 solution",
            SubmittedAt = now
        });
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = team2SolutionId,
            TaskId = taskId,
            TeamId = team2Id,
            SubmittedByUserId = userId2,
            Text = "Team 2 solution",
            SubmittedAt = now
        });

        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId1,
            ReviewerTeamId = team1Id,
            TeamSolutionId = team2SolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetAvailableTeamSolutionsAsync(userId1, taskId);

        result.Records.Should().HaveCount(1);
        result.Records.Single().AlreadyReviewed.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    //  SubmitTeamReviewAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitTeamReview_ShouldThrowNotFound_WhenTeamSolutionNotFound()
    {
        var act = () => _service.SubmitTeamReviewAsync(Guid.NewGuid(), Guid.NewGuid(), new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task SubmitTeamReview_ShouldThrowBadRequest_WhenReviewingOwnTeam()
    {
        var (_, taskId, team1Id, userId1, _, _) = await SeedTeamP2PAsync();

        var now = DateTime.UtcNow;
        var ownSolutionId = Guid.NewGuid();
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = ownSolutionId,
            TaskId = taskId,
            TeamId = team1Id,
            SubmittedByUserId = userId1,
            Text = "Own solution",
            SubmittedAt = now
        });
        await _context.SaveChangesAsync();

        var act = () => _service.SubmitTeamReviewAsync(userId1, ownSolutionId, new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*own team*");
    }

    [Fact]
    public async Task SubmitTeamReview_ShouldThrowEntryExists_WhenAlreadyReviewedSolution()
    {
        var (_, taskId, team1Id, userId1, team2Id, userId2) = await SeedTeamP2PAsync();

        var now = DateTime.UtcNow;
        var team2SolutionId = Guid.NewGuid();
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = team2SolutionId,
            TaskId = taskId,
            TeamId = team2Id,
            SubmittedByUserId = userId2,
            Text = "Team 2 solution",
            SubmittedAt = now
        });

        _context.PeerReviews.Add(new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = userId1,
            ReviewerTeamId = team1Id,
            TeamSolutionId = team2SolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        });
        await _context.SaveChangesAsync();

        var act = () => _service.SubmitTeamReviewAsync(userId1, team2SolutionId, new SubmitPeerReviewDto());

        await act.Should().ThrowAsync<EntryExistsException>();
    }

    [Fact]
    public async Task SubmitTeamReview_ShouldCreateCompletedReview_AndReturnProgress()
    {
        var (_, taskId, team1Id, userId1, team2Id, userId2) = await SeedTeamP2PAsync();

        var now = DateTime.UtcNow;
        var team2SolutionId = Guid.NewGuid();
        _context.TeamSolutions.Add(new TeamSolution
        {
            Id = team2SolutionId,
            TaskId = taskId,
            TeamId = team2Id,
            SubmittedByUserId = userId2,
            Text = "Team 2 solution",
            SubmittedAt = now
        });
        await _context.SaveChangesAsync();

        var progress = await _service.SubmitTeamReviewAsync(userId1, team2SolutionId, new SubmitPeerReviewDto());

        var review = await _context.PeerReviews.FirstAsync();
        review.ReviewerId.Should().Be(userId1);
        review.ReviewerTeamId.Should().Be(team1Id);
        review.TeamSolutionId.Should().Be(team2SolutionId);
        review.Status.Should().Be(PeerReviewStatus.Completed);

        progress.Completed.Should().Be(1);
        progress.Required.Should().Be(1);
        progress.CanFinish.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────
    //  GetTeamProgressAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetTeamProgress_ShouldThrowNotFound_WhenTaskNotFound()
    {
        var act = () => _service.GetTeamProgressAsync(Guid.NewGuid(), Guid.NewGuid());

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetTeamProgress_ShouldReturnZeroCompleted_WhenNoReviewsDone()
    {
        var (_, taskId, _, userId1, _, _) = await SeedTeamP2PAsync();

        var progress = await _service.GetTeamProgressAsync(userId1, taskId);

        progress.Required.Should().Be(1);
        progress.Completed.Should().Be(0);
        progress.CanFinish.Should().BeFalse();
    }

    // ──────────────────────────────────────────────────────────────
    //  GetIndividualProgressOrNullAsync / GetTeamProgressOrNullAsync
    // ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetIndividualProgressOrNull_ShouldReturnNull_WhenTaskNotP2P()
    {
        var taskId = Guid.NewGuid();
        _context.Assignments.Add(new Assignment
        {
            Id = taskId, CourseId = Guid.NewGuid(),
            GradingMode = GradingMode.TeacherReview, MaxScore = 10, Title = "T"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetIndividualProgressOrNullAsync(Guid.NewGuid(), taskId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetTeamProgressOrNull_ShouldReturnNull_WhenTaskNotP2P()
    {
        var taskId = Guid.NewGuid();
        _context.TeamAssignments.Add(new TeamAssignment
        {
            Id = taskId, CourseId = Guid.NewGuid(),
            GradingMode = GradingMode.TeacherReview, MaxScore = 10, Title = "T"
        });
        await _context.SaveChangesAsync();

        var result = await _service.GetTeamProgressOrNullAsync(Guid.NewGuid(), taskId);

        result.Should().BeNull();
    }
}
