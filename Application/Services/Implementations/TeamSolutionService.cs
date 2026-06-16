using Application.DTOs.Grading;
using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
using Domain.Models.Criteria;
using FluentValidation;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.User;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations
{
    public class TeamSolutionService : ITeamSolutionService
    {
        private readonly GcDbContext _context;
        private readonly IValidator<SubmitTeamSolutionRequestDto> _submitValidator;
        private readonly IValidator<UpdateTeamSolutionRequestDto> _updateValidator;
        private readonly IGradeDistributionService _gradeDistributionService;
        private readonly IGradeCalculator _gradeCalculator;
        private readonly IPeerReviewService _peerReviewService;

        public TeamSolutionService(
            GcDbContext context,
            IValidator<SubmitTeamSolutionRequestDto> submitValidator,
            IValidator<UpdateTeamSolutionRequestDto> updateValidator,
            IGradeDistributionService gradeDistributionService,
            IGradeCalculator gradeCalculator,
            IPeerReviewService peerReviewService)
        {
            _context = context;
            _submitValidator = submitValidator;
            _updateValidator = updateValidator;
            _gradeDistributionService = gradeDistributionService;
            _gradeCalculator = gradeCalculator;
            _peerReviewService = peerReviewService;
        }

        public async Task<IdRequestDto> SubmitSolutionAsync(
            Guid currentUserId,
            Guid taskId,
            SubmitTeamSolutionRequestDto dto)
        {
            await _submitValidator.ValidateAndThrowAsync(dto);

            var task = await _context.TeamAssignments
                .Include(t => t.Criteria)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Student)
                throw new ForbiddenException("Only students can submit solutions");

            var (team, isCaptain) = await GetTeamAndCaptainStatusAsync(currentUserId, taskId);
            if (team == null)
                throw new BadRequestException("You must be in a team to submit solution");
            if (!isCaptain)
                throw new ForbiddenException("Only team captain can submit the solution");

            ValidateTeamSize(team, task);
            ValidateDeadline(task);

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            if (task.StudentScoreWeight == 0f && dto.SelfAssessment != null)
                dto.SelfAssessment = null;

            if (dto.SelfAssessment != null)
                ValidateEvaluationAgainstCriteria(task.Criteria, dto.SelfAssessment, isStudent: true);

            var solution = await _context.TeamSolutions
                .Include(s => s.FileTeamSolutions)
                .Include(s => s.WeightedValues)
                .Include(s => s.ToggledValues)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);

            var nowUtc = DateTime.UtcNow;
            if (solution == null)
            {
                solution = new TeamSolution
                {
                    Id = Guid.NewGuid(),
                    TaskId = taskId,
                    TeamId = team.Id,
                    SubmittedByUserId = currentUserId,
                    Text = dto.Text ?? string.Empty,
                    Status = SolutionStatus.Pending,
                    CreatedDate = nowUtc,
                    UpdatedDate = nowUtc,
                    SubmittedAt = nowUtc
                };
                _context.TeamSolutions.Add(solution);
            }
            else
            {
                if (solution.Status == SolutionStatus.Checked)
                    throw new BadRequestException("Cannot resubmit checked solution");

                solution.Text = dto.Text ?? string.Empty;
                solution.Status = SolutionStatus.Pending;
                solution.SubmittedByUserId = currentUserId;
                solution.UpdatedDate = nowUtc;
                solution.SubmittedAt = nowUtc;

                _context.FileTeamSolutions.RemoveRange(solution.FileTeamSolutions);
                solution.FileTeamSolutions.Clear();
            }

            if (dto.SelfAssessment != null)
            {
                var staleSelfWeighted = solution.WeightedValues
                    .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
                _context.WeightedCriterionValues.RemoveRange(staleSelfWeighted);
                foreach (var v in staleSelfWeighted) solution.WeightedValues.Remove(v);

                var staleSelfToggled = solution.ToggledValues
                    .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
                _context.ToggledCriterionValues.RemoveRange(staleSelfToggled);
                foreach (var v in staleSelfToggled) solution.ToggledValues.Remove(v);
            }

            if (dto.Files != null)
            {
                foreach (var fileId in dto.Files)
                {
                    solution.FileTeamSolutions.Add(new FileTeamSolution
                    {
                        Id = Guid.NewGuid(),
                        FileId = fileId,
                        TeamSolutionId = solution.Id
                    });
                }
            }

            if (dto.SelfAssessment != null)
                PersistEvaluationValues(solution, dto.SelfAssessment, currentUserId, isSelfAssessment: true);

            await _context.SaveChangesAsync();

            return new IdRequestDto { Id = solution.Id };
        }

        public async Task<IdRequestDto> DeleteSolutionAsync(Guid currentUserId, Guid taskId)
        {
            var task = await _context.TeamAssignments.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var (team, isCaptain) = await GetTeamAndCaptainStatusAsync(currentUserId, taskId);
            if (team == null)
                throw new BadRequestException("You are not in a team");
            if (!isCaptain)
                throw new ForbiddenException("Only team captain can delete the solution");

            var solution = await _context.TeamSolutions
                .Include(s => s.FileTeamSolutions)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);

            if (solution == null)
                throw new NotFoundException("Solution not found");

            if (solution.Status == SolutionStatus.Checked)
                throw new BadRequestException("Cannot delete checked solution");

            _context.FileTeamSolutions.RemoveRange(solution.FileTeamSolutions);
            _context.TeamSolutions.Remove(solution);
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = taskId };
        }

        public async Task<StudentTeamSolutionDetailsDto> GetMySolutionAsync(Guid currentUserId, Guid taskId)
        {
            var task = await _context.TeamAssignments.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.AssignmentId == taskId && t.Members.Any(m => m.UserId == currentUserId));
            if (team == null)
                throw new NotFoundException("You are not in a team");

            var solution = await _context.TeamSolutions
                .Include(s => s.FileTeamSolutions).ThenInclude(f => f.File)
                .Include(s => s.Team).ThenInclude(t => t.Members).ThenInclude(m => m.User)
                .Include(s => s.SubmittedByUser)
                .Include(s => s.WeightedValues).ThenInclude(v => v.Evaluator)
                .Include(s => s.ToggledValues).ThenInclude(v => v.Evaluator)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);

            if (solution == null)
                throw new NotFoundException("Solution not found");

            var selfAssessments = BuildSelfAssessmentList(solution);
            var teacherWeighted = solution.WeightedValues.Where(v => !v.IsSelfAssessment).ToList();
            var teacherToggled = solution.ToggledValues.Where(v => !v.IsSelfAssessment).ToList();

            return new StudentTeamSolutionDetailsDto
            {
                Id = solution.Id,
                Text = solution.Text,
                Score = solution.Score == 0 ? null : (int)solution.Score,
                Status = solution.Status,
                UpdatedDate = solution.UpdatedDate,
                Team = MapToTeamDto(solution.Team),
                SubmittedBy = new UserCredentialsDto
                {
                    Id = solution.SubmittedByUserId,
                    Credentials = solution.SubmittedByUser.Credentials
                },
                Files = solution.FileTeamSolutions?.Select(f => new FileDto
                {
                    Id = f.FileId.ToString(),
                    Name = f.File.OriginalName
                }).ToList(),
                SelfAssessments = selfAssessments,
                TeacherEvaluation = teacherWeighted.Any() || teacherToggled.Any()
                    ? CriterionMapper.ToEvaluationDto(teacherWeighted, teacherToggled) : null,
                PeerReviewProgress = await _peerReviewService.GetTeamProgressOrNullAsync(currentUserId, taskId)
            };
        }

        public async Task<TeamSolutionListDto> GetSolutionListAsync(
            Guid currentUserId,
            Guid taskId,
            int skip,
            int take,
            SolutionStatus? status,
            Guid? teamId)
        {
            var task = await _context.TeamAssignments.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can view all solutions");

            var query = _context.TeamSolutions
                .Include(s => s.Team).ThenInclude(t => t.Members).ThenInclude(m => m.User)
                .Include(s => s.FileTeamSolutions).ThenInclude(f => f.File)
                .Where(s => s.TaskId == taskId)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(s => s.Status == status.Value);
            if (teamId.HasValue)
                query = query.Where(s => s.TeamId == teamId.Value);

            var totalRecords = await query.CountAsync();
            var records = await query
                .OrderByDescending(s => s.UpdatedDate)
                .Skip(skip)
                .Take(take)
                .Select(s => new TeamSolutionListItemDto
                {
                    Id = s.Id,
                    Text = s.Text,
                    Score = s.Score == 0 ? null : (int)s.Score,
                    Status = s.Status,
                    UpdatedDate = s.UpdatedDate,
                    Team = new TeamDto
                    {
                        Id = s.Team.Id,
                        Name = s.Team.Name,
                        Members = s.Team.Members.Select(m => new TeamMemberDto
                        {
                            UserId = m.UserId,
                            Credentials = m.User.Credentials,
                            Role = m.Role
                        }).ToList()
                    },
                    Files = s.FileTeamSolutions.Select(f => new FileDto
                    {
                        Id = f.FileId.ToString(),
                        Name = f.File.OriginalName
                    }).ToList()
                })
                .ToListAsync();

            return new TeamSolutionListDto { Records = records, TotalRecords = totalRecords };
        }

        public async Task<IdRequestDto> MarkSolutionAsync(
            Guid currentUserId,
            Guid solutionId,
            UpdateTeamSolutionRequestDto dto)
        {
            await _updateValidator.ValidateAndThrowAsync(dto);

            var solution = await _context.TeamSolutions
                .Include(s => s.Task).ThenInclude(t => t.Criteria)
                .Include(s => s.WeightedValues)
                .Include(s => s.ToggledValues)
                .FirstOrDefaultAsync(s => s.Id == solutionId);
            if (solution == null)
                throw new NotFoundException("Solution not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == solution.Task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can review solutions");

            uint oldScore = solution.Score;

            if (dto.Evaluation != null)
            {
                ValidateEvaluationAgainstCriteria(solution.Task.Criteria, dto.Evaluation, isStudent: false);

                var staleWeighted = solution.WeightedValues.Where(v => !v.IsSelfAssessment).ToList();
                _context.WeightedCriterionValues.RemoveRange(staleWeighted);
                foreach (var v in staleWeighted) solution.WeightedValues.Remove(v);

                var staleToggled = solution.ToggledValues.Where(v => !v.IsSelfAssessment).ToList();
                _context.ToggledCriterionValues.RemoveRange(staleToggled);
                foreach (var v in staleToggled) solution.ToggledValues.Remove(v);

                PersistEvaluationValues(solution, dto.Evaluation, currentUserId, isSelfAssessment: false);

                var selfEvals = BuildSelfEvaluationDtos(solution);

                var breakdown = _gradeCalculator.Calculate(BuildCalculatorInput(
                    solution.Task, solution.SubmittedAt, dto.Evaluation, selfEvals));

                solution.Score = (uint)Math.Round(Math.Max(0f, breakdown.FinalScore));
            }
            else if (dto.Score.HasValue)
            {
                if (dto.Score > solution.Task.MaxScore)
                    throw new BadRequestException("Score exceeds max score");

                solution.Score = (uint)dto.Score.Value;
            }

            if (solution.Score != oldScore)
            {
                await _gradeDistributionService.ResetDistributionAsync(solution.TeamId, solution.TaskId, solution.Score);
            }

            solution.Status = dto.Status;
            solution.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = solution.Id };
        }

        public async Task<IdRequestDto> SubmitSelfAssessmentAsync(Guid currentUserId, Guid taskId, SubmitSelfAssessmentDto dto)
        {
            var task = await _context.TeamAssignments
                .Include(t => t.Criteria)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            if (task.StudentScoreWeight == 0f)
                throw new BadRequestException("Self-assessment is disabled for this task");

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.AssignmentId == taskId && t.Members.Any(m => m.UserId == currentUserId));
            if (team == null)
                throw new ForbiddenException("You are not a member of any team for this assignment");

            var solution = await _context.TeamSolutions
                .Include(s => s.WeightedValues)
                .Include(s => s.ToggledValues)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);
            if (solution == null)
                throw new BadRequestException("Captain must submit the solution before members can self-assess");

            if (solution.Status == SolutionStatus.Checked)
                throw new BadRequestException("Cannot modify self-assessment for a checked solution");

            ValidateEvaluationAgainstCriteria(task.Criteria, dto.Evaluation, isStudent: true);

            var staleWeighted = solution.WeightedValues
                .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
            _context.WeightedCriterionValues.RemoveRange(staleWeighted);
            foreach (var v in staleWeighted) solution.WeightedValues.Remove(v);

            var staleToggled = solution.ToggledValues
                .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
            _context.ToggledCriterionValues.RemoveRange(staleToggled);
            foreach (var v in staleToggled) solution.ToggledValues.Remove(v);

            PersistEvaluationValues(solution, dto.Evaluation, currentUserId, isSelfAssessment: true);

            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = solution.Id };
        }

        public async Task<IdRequestDto> DeleteSelfAssessmentAsync(Guid currentUserId, Guid taskId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.AssignmentId == taskId && t.Members.Any(m => m.UserId == currentUserId));
            if (team == null)
                throw new ForbiddenException("You are not a member of any team for this assignment");

            var solution = await _context.TeamSolutions
                .Include(s => s.WeightedValues)
                .Include(s => s.ToggledValues)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);
            if (solution == null)
                throw new NotFoundException("Solution not found");

            if (solution.Status == SolutionStatus.Checked)
                throw new BadRequestException("Cannot modify self-assessment for a checked solution");

            var staleWeighted = solution.WeightedValues
                .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
            _context.WeightedCriterionValues.RemoveRange(staleWeighted);
            foreach (var v in staleWeighted) solution.WeightedValues.Remove(v);

            var staleToggled = solution.ToggledValues
                .Where(v => v.IsSelfAssessment && v.EvaluatorUserId == currentUserId).ToList();
            _context.ToggledCriterionValues.RemoveRange(staleToggled);
            foreach (var v in staleToggled) solution.ToggledValues.Remove(v);

            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = solution.Id };
        }

        public async Task<GradeBreakdownDto> PreviewScoreAsync(Guid currentUserId, Guid solutionId, GradePreviewRequestDto dto)
        {
            var solution = await _context.TeamSolutions
                .Include(s => s.Task).ThenInclude(t => t.Criteria)
                .Include(s => s.WeightedValues)
                .Include(s => s.ToggledValues)
                .FirstOrDefaultAsync(s => s.Id == solutionId);
            if (solution == null)
                throw new NotFoundException("Solution not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == solution.Task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can preview");

            ValidateEvaluationAgainstCriteria(solution.Task.Criteria, dto.Evaluation, isStudent: false);

            var selfEvals = BuildSelfEvaluationDtos(solution);

            return _gradeCalculator.Calculate(BuildCalculatorInput(
                solution.Task, solution.SubmittedAt, dto.Evaluation, selfEvals));
        }

        #region Private Helpers

        private static IReadOnlyList<EvaluationDto> BuildSelfEvaluationDtos(TeamSolution solution)
        {
            return solution.WeightedValues.Where(v => v.IsSelfAssessment)
                .Select(v => v.EvaluatorUserId)
                .Concat(solution.ToggledValues.Where(v => v.IsSelfAssessment).Select(v => v.EvaluatorUserId))
                .Distinct()
                .Select(uid => CriterionMapper.ToEvaluationDto(
                    solution.WeightedValues.Where(v => v.IsSelfAssessment && v.EvaluatorUserId == uid),
                    solution.ToggledValues.Where(v => v.IsSelfAssessment && v.EvaluatorUserId == uid)))
                .ToList();
        }

        private static List<MemberSelfAssessmentDto> BuildSelfAssessmentList(TeamSolution solution)
        {
            return solution.WeightedValues.Where(v => v.IsSelfAssessment)
                .Select(v => v.EvaluatorUserId)
                .Concat(solution.ToggledValues.Where(v => v.IsSelfAssessment).Select(v => v.EvaluatorUserId))
                .Distinct()
                .Select(uid =>
                {
                    var member = solution.Team.Members.FirstOrDefault(m => m.UserId == uid);
                    return new MemberSelfAssessmentDto
                    {
                        UserId = uid,
                        Credentials = member?.User?.Credentials ?? string.Empty,
                        Evaluation = CriterionMapper.ToEvaluationDto(
                            solution.WeightedValues.Where(v => v.IsSelfAssessment && v.EvaluatorUserId == uid),
                            solution.ToggledValues.Where(v => v.IsSelfAssessment && v.EvaluatorUserId == uid))
                    };
                })
                .ToList();
        }

        private GradeCalculationInput BuildCalculatorInput(
            TeamAssignment task,
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
            TeamSolution solution,
            EvaluationDto eval,
            Guid evaluatorId,
            bool isSelfAssessment)
        {
            foreach (var w in eval.WeightedValues)
            {
                _context.WeightedCriterionValues.Add(new WeightedCriterionValue
                {
                    Id = Guid.NewGuid(),
                    CriterionId = w.CriterionId,
                    TeamSolutionId = solution.Id,
                    EvaluatorUserId = evaluatorId,
                    IsSelfAssessment = isSelfAssessment,
                    Score = w.Score,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }

            foreach (var t in eval.ToggledValues)
            {
                _context.ToggledCriterionValues.Add(new ToggledCriterionValue
                {
                    Id = Guid.NewGuid(),
                    CriterionId = t.CriterionId,
                    TeamSolutionId = solution.Id,
                    EvaluatorUserId = evaluatorId,
                    IsSelfAssessment = isSelfAssessment,
                    Enabled = t.Enabled,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }
        }

        private async Task<(Team? team, bool isCaptain)> GetTeamAndCaptainStatusAsync(Guid userId, Guid taskId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.AssignmentId == taskId && t.Members.Any(m => m.UserId == userId));
            if (team == null) return (null, false);
            var isCaptain = team.Members.Any(m => m.UserId == userId && m.Role == TeamMemberRole.Leader)
                            || team.FixedCaptainId == userId;
            return (team, isCaptain);
        }

        private async Task<Team?> GetTeamForStudent(Guid userId, Guid courseId)
        {
            return await _context.Teams
                .Include(t => t.Members)
                .Where(t => t.CourseId == courseId && t.Members.Any(m => m.UserId == userId))
                .FirstOrDefaultAsync();
        }

        private void ValidateTeamSize(Team team, TeamAssignment task)
        {
            var memberCount = team.Members.Count;

            if (memberCount < task.MinTeamSize)
                throw new BadRequestException($"Team must have at least {task.MinTeamSize} members");

            if (memberCount > task.MaxTeamSize)
                throw new BadRequestException($"Team must have at most {task.MaxTeamSize} members");
        }

        private void ValidateDeadline(TeamAssignment task)
        {
            if (task.Deadline.HasValue && DateTime.UtcNow > task.Deadline.Value && !task.SolvableAfterDeadline)
                throw new BadRequestException("Deadline has passed");

        }

        private async Task ValidateFilesExist(IEnumerable<Guid> fileIds)
        {
            var fileIdsList = fileIds.ToList();
            var existing = await _context.UserFiles.CountAsync(f => fileIdsList.Contains(f.Id));
            if (existing != fileIdsList.Count)
                throw new NotFoundException("One or more files not found");
        }

        private TeamDto MapToTeamDto(Team team)
        {
            return new TeamDto
            {
                Id = team.Id,
                Name = team.Name,
                Members = team.Members.Select(m => new TeamMemberDto
                {
                    UserId = m.UserId,
                    Credentials = m.User.Credentials,
                    Role = m.Role
                }).ToList()
            };
        }

        #endregion
    }
}
