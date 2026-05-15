using Application.DTOs.Grading;
using Application.DTOs.Post;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Domain.Models;
using Domain.Models.Criteria;
using FluentValidation;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.User;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Services.Implementations;

public class SolutionService : ISolutionService
{
    private readonly GcDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IValidator<SubmitSolutionRequestDto> _submitValidator;
    private readonly IValidator<UpdateSolutionRequestDto> _updateValidator;
    private readonly IGradeCalculator _gradeCalculator;

    public SolutionService(
        GcDbContext context,
        UserManager<User> userManager,
        IMapper mapper,
        IValidator<SubmitSolutionRequestDto> submitValidator,
        IValidator<UpdateSolutionRequestDto> updateValidator,
        IGradeCalculator gradeCalculator)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _submitValidator = submitValidator;
        _updateValidator = updateValidator;
        _gradeCalculator = gradeCalculator;
    }

    public async Task<IdRequestDto> SubmitSolutionAsync(Guid currentUserId, Guid taskId, SubmitSolutionRequestDto dto)
    {
        await _submitValidator.ValidateAndThrowAsync(dto);

        var task = await _context.Assignments
            .Include(a => a.Criteria)
            .FirstOrDefaultAsync(a => a.Id == taskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);

        if (role == null || role.RoleType != UserRoleType.Student)
            throw new ForbiddenException("Only students can submit solutions");

        if (dto.Files != null && dto.Files.Any())
            await ValidateFilesExistAsync(dto.Files);

        if (task.StudentScoreWeight > 0f && dto.SelfAssessment == null)
            throw new BadRequestException("Self-assessment is required for this task");
        if (task.StudentScoreWeight == 0f && dto.SelfAssessment != null)
            dto.SelfAssessment = null;

        if (dto.SelfAssessment != null)
            ValidateEvaluationAgainstCriteria(task.Criteria, dto.SelfAssessment, isStudent: true);

        var solution = await _context.Solutions
            .Include(s => s.FileSolutions)
            .Include(s => s.WeightedValues)
            .Include(s => s.ToggledValues)
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);

        var nowUtc = DateTime.UtcNow;
        if (solution == null)
        {
            solution = new Solution
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = currentUserId,
                Text = dto.Text ?? "",
                Status = SolutionStatus.Pending,
                CreatedDate = nowUtc,
                UpdatedDate = nowUtc,
                SubmittedAt = nowUtc
            };

            _context.Solutions.Add(solution);
        }
        else
        {
            solution.Text = dto.Text ?? "";
            solution.Status = SolutionStatus.Pending;
            solution.UpdatedDate = nowUtc;
            solution.SubmittedAt = nowUtc;

            _context.FileSolutions.RemoveRange(solution.FileSolutions);
            solution.FileSolutions.Clear();

            var staleSelf = solution.WeightedValues.Where(v => v.IsSelfAssessment).ToList();
            _context.WeightedCriterionValues.RemoveRange(staleSelf);
            foreach (var v in staleSelf) solution.WeightedValues.Remove(v);

            var staleToggledSelf = solution.ToggledValues.Where(v => v.IsSelfAssessment).ToList();
            _context.ToggledCriterionValues.RemoveRange(staleToggledSelf);
            foreach (var v in staleToggledSelf) solution.ToggledValues.Remove(v);
        }

        if (dto.Files != null)
        {
            foreach (var fileId in dto.Files)
            {
                solution.FileSolutions.Add(new FileSolution
                {
                    Id = Guid.NewGuid(),
                    FileId = fileId,
                    SolutionId = solution.Id
                });
            }
        }

        if (dto.SelfAssessment != null)
            PersistEvaluationValues(solution, null, dto.SelfAssessment, currentUserId, isSelfAssessment: true);

        await _context.SaveChangesAsync();

        return new IdRequestDto
        {
            Id = solution.Id
        };
    }

    public async Task<IdRequestDto> DeleteSolutionAsync(Guid currentUserId, Guid taskId)
    {
        var solution = await _context.Solutions
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);

        if (solution == null)
            throw new NotFoundException("Solution not found");

        _context.Solutions.Remove(solution);

        await _context.SaveChangesAsync();

        return new IdRequestDto
        {
            Id = taskId
        };
    }

    public async Task<StudentSolutionDetailsDto> GetSolutionByIdAsync(Guid currentUserId, Guid taskId)
    {
        var solution = await _context.Solutions
            .Include(s => s.FileSolutions).ThenInclude(fp => fp.File)
            .Include(s => s.Task).ThenInclude(t => t!.Criteria)
            .Include(s => s.WeightedValues)
            .Include(s => s.ToggledValues)
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);

        if (solution == null)
            throw new NotFoundException("Solution not found");

        var selfWeighted = solution.WeightedValues.Where(v => v.IsSelfAssessment).ToList();
        var selfToggled = solution.ToggledValues.Where(v => v.IsSelfAssessment).ToList();
        var teacherWeighted = solution.WeightedValues.Where(v => !v.IsSelfAssessment).ToList();
        var teacherToggled = solution.ToggledValues.Where(v => !v.IsSelfAssessment).ToList();

        return new StudentSolutionDetailsDto
        {
            Id = solution.Id,
            Text = solution.Text,
            Files = solution.FileSolutions?.Select(fp => new FileDto
            {
                Id = fp.FileId.ToString(),
                Name = fp.File.OriginalName
            }).ToList(),
            Score = solution.Score == 0 ? null : (int)solution.Score,
            Status = solution.Status,
            UpdatedDate = solution.UpdatedDate,
            SelfAssessment = selfWeighted.Any() || selfToggled.Any()
                ? CriterionMapper.ToEvaluationDto(selfWeighted, selfToggled) : null,
            TeacherEvaluation = teacherWeighted.Any() || teacherToggled.Any()
                ? CriterionMapper.ToEvaluationDto(teacherWeighted, teacherToggled) : null
        };
    }

    public async Task<SolutionListDto> GetSolutionListAsync(
        Guid currentUserId,
        Guid taskId,
        int skip,
        int take,
        SolutionStatus? status,
        Guid? studentId)
    {
        var task = await _context.Assignments
            .Include(t => t.Course)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            throw new NotFoundException("Task not found");

        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);

        if (role == null || role.RoleType != UserRoleType.Teacher)
            throw new ForbiddenException("Only teachers can view solutions");

        var query = _context.Solutions
            .Include(s => s.User)
            .Include(s => s.FileSolutions)
            .Where(s => s.TaskId == taskId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        if (studentId.HasValue)
            query = query.Where(s => s.UserId == studentId.Value);

        var totalRecords = await query.CountAsync();

        var records = await query
            .OrderByDescending(s => s.UpdatedDate)
            .Skip(skip)
            .Take(take)
            .Select(s => new SolutionListItemDto
            {
                Id = s.Id,
                User = new UserCredentialsDto
                {
                    Id = s.User.Id,
                    Credentials = s.User.Credentials
                },
                Text = s.Text,
                Score = s.Score == 0 ? null : (int)s.Score,
                Status = s.Status,
                Files = s.FileSolutions.Select(fp => new FileDto
                {
                    Id = fp.FileId.ToString(),
                    Name = fp.File.OriginalName
                }).ToList(),
                UpdatedDate = s.UpdatedDate
            })
            .ToListAsync();

        return new SolutionListDto
        {
            Records = records,
            TotalRecords = totalRecords
        };
    }

    public async Task<IdRequestDto> MarkSolutionAsync(Guid currentUserId, Guid solutionId, UpdateSolutionRequestDto dto)
    {
        await _updateValidator.ValidateAndThrowAsync(dto);

        var solution = await _context.Solutions
            .Include(s => s.Task).ThenInclude(t => t!.Criteria)
            .Include(s => s.WeightedValues)
            .Include(s => s.ToggledValues)
            .FirstOrDefaultAsync(s => s.Id == solutionId);

        if (solution == null)
            throw new NotFoundException("Solution not found");

        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == solution.Task!.CourseId && r.UserId == currentUserId);

        if (role == null || role.RoleType != UserRoleType.Teacher)
            throw new ForbiddenException("Only teachers can review solutions");

        if (dto.Evaluation != null)
        {
            ValidateEvaluationAgainstCriteria(solution.Task!.Criteria, dto.Evaluation, isStudent: false);

            var staleTeacher = solution.WeightedValues.Where(v => !v.IsSelfAssessment).ToList();
            _context.WeightedCriterionValues.RemoveRange(staleTeacher);
            foreach (var v in staleTeacher) solution.WeightedValues.Remove(v);

            var staleToggledTeacher = solution.ToggledValues.Where(v => !v.IsSelfAssessment).ToList();
            _context.ToggledCriterionValues.RemoveRange(staleToggledTeacher);
            foreach (var v in staleToggledTeacher) solution.ToggledValues.Remove(v);

            PersistEvaluationValues(solution, null, dto.Evaluation, currentUserId, isSelfAssessment: false);

            var selfWeighted = solution.WeightedValues.Where(v => v.IsSelfAssessment).ToList();
            var selfToggled = solution.ToggledValues.Where(v => v.IsSelfAssessment).ToList();

            var selfEvalDto = (selfWeighted.Any() || selfToggled.Any())
                ? CriterionMapper.ToEvaluationDto(selfWeighted, selfToggled) : null;

            var breakdown = _gradeCalculator.Calculate(BuildCalculatorInput(
                solution.Task!, solution.SubmittedAt, dto.Evaluation,
                selfEvalDto != null ? new[] { selfEvalDto } : Array.Empty<EvaluationDto>()));

            solution.Score = (uint)Math.Round(Math.Max(0f, breakdown.FinalScore));
        }
        else if (dto.Score.HasValue)
        {
            if (dto.Score > solution.Task!.MaxScore)
                throw new BadRequestException("Score exceeds task max score");

            solution.Score = (uint)dto.Score.Value;
        }

        solution.Status = dto.Status;
        solution.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new IdRequestDto
        {
            Id = solution.Id
        };
    }

    public async Task<GradeBreakdownDto> PreviewScoreAsync(Guid currentUserId, Guid solutionId, GradePreviewRequestDto dto)
    {
        var solution = await _context.Solutions
            .Include(s => s.Task).ThenInclude(t => t!.Criteria)
            .Include(s => s.WeightedValues)
            .Include(s => s.ToggledValues)
            .FirstOrDefaultAsync(s => s.Id == solutionId);

        if (solution == null)
            throw new NotFoundException("Solution not found");

        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == solution.Task!.CourseId && r.UserId == currentUserId);

        if (role == null || role.RoleType != UserRoleType.Teacher)
            throw new ForbiddenException("Only teachers can preview");

        ValidateEvaluationAgainstCriteria(solution.Task!.Criteria, dto.Evaluation, isStudent: false);

        var selfWeighted = solution.WeightedValues.Where(v => v.IsSelfAssessment).ToList();
        var selfToggled = solution.ToggledValues.Where(v => v.IsSelfAssessment).ToList();
        var selfEvalDto = (selfWeighted.Any() || selfToggled.Any())
            ? CriterionMapper.ToEvaluationDto(selfWeighted, selfToggled) : null;

        return _gradeCalculator.Calculate(BuildCalculatorInput(
            solution.Task!, solution.SubmittedAt, dto.Evaluation,
            selfEvalDto != null ? new[] { selfEvalDto } : Array.Empty<EvaluationDto>()));
    }

    private GradeCalculationInput BuildCalculatorInput(
        Assignment task,
        DateTime solutionTimestamp,
        EvaluationDto teacherEvaluation,
        IReadOnlyList<EvaluationDto> selfEvaluations)
    {
        return new GradeCalculationInput
        {
            MaxScore = task.MaxScore,
            FailThreshold = task.FailThreshold,
            SuccessThreshold = task.SuccessThreshold,
            StudentScoreWeight = task.StudentScoreWeight,
            PenaltyPerDay = task.PenaltyPerDay,
            MaxDays = task.MaxDays,
            Deadline = task.Deadline,
            SolutionTimestamp = solutionTimestamp,
            Criteria = task.Criteria.ToList(),
            TeacherEvaluation = CriterionMapper.ToCalculatorInput(teacherEvaluation),
            SelfEvaluations = selfEvaluations.Select(CriterionMapper.ToCalculatorInput).ToList()
        };
    }

    private static void ValidateEvaluationAgainstCriteria(
        IEnumerable<Criterion> criteria,
        EvaluationDto evaluation,
        bool isStudent)
    {
        var weightedIds = criteria.OfType<WeightedCriterion>().Select(c => c.Id).ToHashSet();
        var bpIds = criteria.OfType<BonusPenaltyCriterion>().Select(c => c.Id).ToHashSet();
        var blockingIds = criteria.OfType<BlockingModifier>().Select(c => c.Id).ToHashSet();
        var allToggled = bpIds.Concat(blockingIds).ToHashSet();

        foreach (var v in evaluation.WeightedValues)
        {
            if (!weightedIds.Contains(v.CriterionId))
                throw new BadRequestException($"Unknown or non-weighted criterion {v.CriterionId}");
            if (v.Score < 0)
                throw new BadRequestException("Score cannot be negative");

            var criterion = criteria.OfType<WeightedCriterion>().First(c => c.Id == v.CriterionId);
            if (v.Score > criterion.MaxScore)
                throw new BadRequestException($"Score for '{criterion.Title}' exceeds max ({criterion.MaxScore})");
        }

        foreach (var v in evaluation.ToggledValues)
        {
            if (isStudent)
            {
                if (!bpIds.Contains(v.CriterionId))
                    throw new BadRequestException($"Students can only toggle bonus/penalty criteria; got {v.CriterionId}");
            }
            else if (!allToggled.Contains(v.CriterionId))
            {
                throw new BadRequestException($"Unknown toggled criterion {v.CriterionId}");
            }
        }
    }

    private void PersistEvaluationValues(
        Solution? solution,
        TeamSolution? teamSolution,
        EvaluationDto eval,
        Guid evaluatorId,
        bool isSelfAssessment)
    {
        foreach (var w in eval.WeightedValues)
        {
            var entity = new WeightedCriterionValue
            {
                Id = Guid.NewGuid(),
                CriterionId = w.CriterionId,
                SolutionId = solution?.Id,
                TeamSolutionId = teamSolution?.Id,
                EvaluatorUserId = evaluatorId,
                IsSelfAssessment = isSelfAssessment,
                Score = w.Score,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.WeightedCriterionValues.Add(entity);
        }

        foreach (var t in eval.ToggledValues)
        {
            var entity = new ToggledCriterionValue
            {
                Id = Guid.NewGuid(),
                CriterionId = t.CriterionId,
                SolutionId = solution?.Id,
                TeamSolutionId = teamSolution?.Id,
                EvaluatorUserId = evaluatorId,
                IsSelfAssessment = isSelfAssessment,
                Enabled = t.Enabled,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.ToggledCriterionValues.Add(entity);
        }
    }

    private async Task ValidateFilesExistAsync(IEnumerable<Guid> fileIds)
    {
        var fileIdsList = fileIds.ToList();
        var existing = await _context.UserFiles.CountAsync(f => fileIdsList.Contains(f.Id));
        if (existing != fileIdsList.Count)
            throw new NotFoundException("One or more files not found");
    }
}
