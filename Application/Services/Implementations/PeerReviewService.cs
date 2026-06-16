using Application.DTOs.Grading;
using Application.DTOs.Grading.PeerReview;
using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
using Domain.Models.Criteria;
using FluentValidation;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations;

public class PeerReviewService : IPeerReviewService
{
    private readonly GcDbContext _context;
    private readonly IValidator<SubmitPeerReviewDto> _submitValidator;

    public PeerReviewService(GcDbContext context, IValidator<SubmitPeerReviewDto> submitValidator)
    {
        _context = context;
        _submitValidator = submitValidator;
    }

    public async Task<PeerReviewTargetDto?> GetNextAssignmentAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.Assignments
            .Include(a => a.Criteria)
            .FirstOrDefaultAsync(a => a.Id == taskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        EnsureIndividualP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        var ownSolution = await _context.Solutions
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);
        if (ownSolution == null)
            throw new BadRequestException("Submit your own solution before reviewing others");

        var existing = await _context.PeerReviews
            .Include(pr => pr.Solution).ThenInclude(s => s!.FileSolutions).ThenInclude(f => f.File)
            .FirstOrDefaultAsync(pr => pr.TaskId == taskId
                                       && pr.ReviewerId == currentUserId
                                       && pr.Status == PeerReviewStatus.Assigned);
        if (existing != null)
            return BuildTarget(existing, task, existing.Solution!);

        var reviewedSolutionIds = await _context.PeerReviews
            .Where(pr => pr.TaskId == taskId && pr.ReviewerId == currentUserId && pr.SolutionId != null)
            .Select(pr => pr.SolutionId!.Value)
            .ToListAsync();

        var candidateIds = await _context.Solutions
            .Where(s => s.TaskId == taskId
                        && s.UserId != currentUserId
                        && !reviewedSolutionIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync();

        if (candidateIds.Count == 0)
            return null;

        var chosenId = candidateIds[Random.Shared.Next(candidateIds.Count)];

        var now = DateTime.UtcNow;
        var review = new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            ReviewerId = currentUserId,
            SolutionId = chosenId,
            Status = PeerReviewStatus.Assigned,
            AssignedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        };
        _context.PeerReviews.Add(review);
        await _context.SaveChangesAsync();

        var chosen = await _context.Solutions
            .Include(s => s.FileSolutions).ThenInclude(f => f.File)
            .FirstAsync(s => s.Id == chosenId);

        return BuildTarget(review, task, chosen);
    }

    public async Task<PeerReviewProgressDto> SubmitReviewAsync(Guid currentUserId, Guid reviewId, SubmitPeerReviewDto dto)
    {
        await _submitValidator.ValidateAndThrowAsync(dto);

        var review = await _context.PeerReviews
            .Include(pr => pr.WeightedValues)
            .Include(pr => pr.ToggledValues)
            .FirstOrDefaultAsync(pr => pr.Id == reviewId);
        if (review == null)
            throw new NotFoundException("Peer review not found");

        if (review.ReviewerId != currentUserId)
            throw new ForbiddenException("This review assignment belongs to another student");

        if (review.Status == PeerReviewStatus.Completed)
            throw new BadRequestException("This review is already completed");

        var task = await _context.Assignments
            .Include(a => a.Criteria)
            .FirstOrDefaultAsync(a => a.Id == review.TaskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        ValidateEvaluationAgainstCriteria(task.Criteria, dto.Evaluation);

        PersistEvaluationValues(review, dto.Evaluation, currentUserId);

        var now = DateTime.UtcNow;
        review.Status = PeerReviewStatus.Completed;
        review.CompletedAt = now;
        review.UpdatedDate = now;

        await _context.SaveChangesAsync();

        return await ComputeIndividualProgressAsync(task, currentUserId);
    }

    public async Task<PeerReviewProgressDto> GetIndividualProgressAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == taskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        EnsureIndividualP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        return await ComputeIndividualProgressAsync(task, currentUserId);
    }

    public async Task<PeerReviewProgressDto> FinishAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == taskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        EnsureIndividualP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        var solution = await _context.Solutions
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);
        if (solution == null)
            throw new BadRequestException("No attached solution to count");

        var required = task.MinPeerReviewsRequired ?? 0;
        var completed = await CountCompletedIndividualAsync(taskId, currentUserId);

        if (completed < required)
            throw new BadRequestException($"Minimum reviews not completed ({completed}/{required})");

        solution.PeerReviewCounted = true;
        solution.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await ComputeIndividualProgressAsync(task, currentUserId);
    }

    public async Task<PeerReviewProgressDto?> GetIndividualProgressOrNullAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == taskId);
        if (task == null || task.GradingMode != GradingMode.PeerToPeer)
            return null;

        return await ComputeIndividualProgressAsync(task, currentUserId);
    }

    public async Task<PagedResponse<PeerReviewTeamTargetDto>> GetAvailableTeamSolutionsAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.TeamAssignments.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new NotFoundException("Team assignment not found");

        EnsureTeamP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        var team = await GetStudentTeamAsync(currentUserId, taskId);
        if (team == null)
            throw new ForbiddenException("You are not in a team for this assignment");

        var reviewedTeamSolutionIds = await _context.PeerReviews
            .Where(pr => pr.TaskId == taskId && pr.ReviewerId == currentUserId && pr.TeamSolutionId != null)
            .Select(pr => pr.TeamSolutionId!.Value)
            .ToListAsync();

        var records = await _context.TeamSolutions
            .Include(s => s.Team)
            .Where(s => s.TaskId == taskId && s.TeamId != team.Id)
            .OrderBy(s => s.SubmittedAt)
            .Select(s => new PeerReviewTeamTargetDto
            {
                TeamSolutionId = s.Id,
                TeamName = s.Team.Name,
                SubmittedAt = s.SubmittedAt,
                AlreadyReviewed = reviewedTeamSolutionIds.Contains(s.Id)
            })
            .ToListAsync();

        return new PagedResponse<PeerReviewTeamTargetDto>
        {
            Records = records,
            TotalRecords = records.Count
        };
    }

    public async Task<PeerReviewProgressDto> SubmitTeamReviewAsync(Guid currentUserId, Guid teamSolutionId, SubmitPeerReviewDto dto)
    {
        await _submitValidator.ValidateAndThrowAsync(dto);

        var teamSolution = await _context.TeamSolutions
            .FirstOrDefaultAsync(s => s.Id == teamSolutionId);
        if (teamSolution == null)
            throw new NotFoundException("Team solution not found");

        var task = await _context.TeamAssignments
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.Id == teamSolution.TaskId);
        if (task == null)
            throw new NotFoundException("Team assignment not found");

        EnsureTeamP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        var team = await GetStudentTeamAsync(currentUserId, task.Id);
        if (team == null)
            throw new ForbiddenException("You are not in a team for this assignment");

        if (teamSolution.TeamId == team.Id)
            throw new BadRequestException("You cannot review your own team's solution");

        var alreadyReviewed = await _context.PeerReviews
            .AnyAsync(pr => pr.TaskId == task.Id
                            && pr.ReviewerId == currentUserId
                            && pr.TeamSolutionId == teamSolutionId);
        if (alreadyReviewed)
            throw new EntryExistsException("You have already reviewed this team's solution");

        ValidateEvaluationAgainstCriteria(task.Criteria, dto.Evaluation);

        var now = DateTime.UtcNow;
        var review = new PeerReview
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            ReviewerId = currentUserId,
            ReviewerTeamId = team.Id,
            TeamSolutionId = teamSolutionId,
            Status = PeerReviewStatus.Completed,
            AssignedAt = now,
            CompletedAt = now,
            CreatedDate = now,
            UpdatedDate = now
        };
        _context.PeerReviews.Add(review);

        PersistEvaluationValues(review, dto.Evaluation, currentUserId);

        await _context.SaveChangesAsync();

        return await ComputeTeamProgressAsync(task, team, currentUserId);
    }

    public async Task<PeerReviewProgressDto> GetTeamProgressAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.TeamAssignments.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null)
            throw new NotFoundException("Team assignment not found");

        EnsureTeamP2P(task);
        await EnsureStudentAsync(currentUserId, task.CourseId);

        var team = await GetStudentTeamAsync(currentUserId, taskId);
        return await ComputeTeamProgressAsync(task, team, currentUserId);
    }

    public async Task<PeerReviewProgressDto?> GetTeamProgressOrNullAsync(Guid currentUserId, Guid taskId)
    {
        var task = await _context.TeamAssignments.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null || task.GradingMode != GradingMode.PeerToPeer)
            return null;

        var team = await GetStudentTeamAsync(currentUserId, taskId);
        return await ComputeTeamProgressAsync(task, team, currentUserId);
    }

    private async Task<PeerReviewProgressDto> ComputeIndividualProgressAsync(Assignment task, Guid userId)
    {
        var solution = await _context.Solutions
            .FirstOrDefaultAsync(s => s.TaskId == task.Id && s.UserId == userId);

        var required = task.MinPeerReviewsRequired ?? 0;
        var completed = await CountCompletedIndividualAsync(task.Id, userId);

        return new PeerReviewProgressDto
        {
            GradingMode = GradingMode.PeerToPeer,
            Required = required,
            Completed = completed,
            CanFinish = completed >= required,
            IsCounted = solution != null && solution.PeerReviewCounted
        };
    }

    private async Task<PeerReviewProgressDto> ComputeTeamProgressAsync(TeamAssignment task, Team? team, Guid userId)
    {
        var completed = await _context.PeerReviews
            .CountAsync(pr => pr.TaskId == task.Id
                              && pr.ReviewerId == userId
                              && pr.TeamSolutionId != null
                              && pr.Status == PeerReviewStatus.Completed);

        var hasTeamSolution = team != null && await _context.TeamSolutions
            .AnyAsync(s => s.TaskId == task.Id && s.TeamId == team.Id);

        const int required = 1;

        return new PeerReviewProgressDto
        {
            GradingMode = GradingMode.PeerToPeer,
            Required = required,
            Completed = completed,
            CanFinish = completed >= required,
            IsCounted = hasTeamSolution && completed >= required
        };
    }

    private Task<int> CountCompletedIndividualAsync(Guid taskId, Guid userId)
    {
        return _context.PeerReviews
            .CountAsync(pr => pr.TaskId == taskId
                              && pr.ReviewerId == userId
                              && pr.SolutionId != null
                              && pr.Status == PeerReviewStatus.Completed);
    }

    private Task<Team?> GetStudentTeamAsync(Guid userId, Guid taskId)
    {
        return _context.Teams
            .FirstOrDefaultAsync(t => t.AssignmentId == taskId && t.Members.Any(m => m.UserId == userId));
    }

    private async Task EnsureStudentAsync(Guid userId, Guid courseId)
    {
        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);
        if (role == null)
            throw new ForbiddenException("You are not a member of this course");
        if (role.RoleType != UserRoleType.Student)
            throw new ForbiddenException("Only students can take part in peer review");
    }

    private static void EnsureIndividualP2P(Assignment task)
    {
        if (task.GradingMode != GradingMode.PeerToPeer)
            throw new BadRequestException("Peer review is not enabled for this task");
    }

    private static void EnsureTeamP2P(TeamAssignment task)
    {
        if (task.GradingMode != GradingMode.PeerToPeer)
            throw new BadRequestException("Peer review is not enabled for this task");
    }

    private static PeerReviewTargetDto BuildTarget(PeerReview review, Assignment task, Solution solution)
    {
        return new PeerReviewTargetDto
        {
            ReviewId = review.Id,
            TaskId = task.Id,
            Solution = new AnonymizedSolutionDto
            {
                Text = solution.Text,
                Files = solution.FileSolutions?.Select(f => new FileDto
                {
                    Id = f.FileId.ToString(),
                    Name = f.File.OriginalName
                }).ToList()
            },
            Criteria = task.Criteria
                .OrderBy(c => c.OrderIndex)
                .Select(CriterionMapper.ToDto)
                .ToList(),
            AssignedAt = review.AssignedAt
        };
    }

    private void PersistEvaluationValues(PeerReview review, EvaluationDto eval, Guid evaluatorId)
    {
        var now = DateTime.UtcNow;

        foreach (var w in eval.WeightedValues)
        {
            _context.WeightedCriterionValues.Add(new WeightedCriterionValue
            {
                Id = Guid.NewGuid(),
                CriterionId = w.CriterionId,
                PeerReviewId = review.Id,
                EvaluatorUserId = evaluatorId,
                IsSelfAssessment = false,
                Score = w.Score,
                CreatedDate = now,
                UpdatedDate = now
            });
        }

        foreach (var t in eval.ToggledValues)
        {
            _context.ToggledCriterionValues.Add(new ToggledCriterionValue
            {
                Id = Guid.NewGuid(),
                CriterionId = t.CriterionId,
                PeerReviewId = review.Id,
                EvaluatorUserId = evaluatorId,
                IsSelfAssessment = false,
                Enabled = t.Enabled,
                CreatedDate = now,
                UpdatedDate = now
            });
        }
    }

    private static void ValidateEvaluationAgainstCriteria(IEnumerable<Criterion> criteria, EvaluationDto evaluation)
    {
        var weightedById = criteria.OfType<WeightedCriterion>().ToDictionary(c => c.Id);
        var bpIds = criteria.OfType<BonusPenaltyCriterion>().Select(c => c.Id).ToHashSet();
        var blockingIds = criteria.OfType<BlockingModifier>().Select(c => c.Id).ToHashSet();
        var allToggled = bpIds.Concat(blockingIds).ToHashSet();

        foreach (var v in evaluation.WeightedValues)
        {
            if (!weightedById.TryGetValue(v.CriterionId, out var c))
                throw new BadRequestException($"Unknown or non-weighted criterion {v.CriterionId}");
            if (v.Score < 0)
                throw new BadRequestException("Score cannot be negative");
            if (v.Score > c.MaxScore)
                throw new BadRequestException($"Score for '{c.Title}' exceeds max ({c.MaxScore})");
        }

        foreach (var v in evaluation.ToggledValues)
        {
            if (!allToggled.Contains(v.CriterionId))
                throw new BadRequestException($"Unknown toggled criterion {v.CriterionId}");
        }
    }
}
