using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
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

        public TeamSolutionService(
            GcDbContext context,
            IValidator<SubmitTeamSolutionRequestDto> submitValidator,
            IValidator<UpdateTeamSolutionRequestDto> updateValidator,
            IGradeDistributionService gradeDistributionService)
        {
            _context = context;
            _submitValidator = submitValidator;
            _updateValidator = updateValidator;
            _gradeDistributionService = gradeDistributionService;
        }

        public async Task<IdRequestDto> SubmitSolutionAsync(
            Guid currentUserId,
            Guid taskId,
            SubmitTeamSolutionRequestDto dto)
        {
            await _submitValidator.ValidateAndThrowAsync(dto);

            var task = await _context.TeamAssignments.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Student)
                throw new ForbiddenException("Only students can submit solutions");

            var (team, isCaptain) = await GetTeamAndCaptainStatusAsync(currentUserId, task.CourseId);
            if (team == null)
                throw new BadRequestException("You must be in a team to submit solution");
            if (!isCaptain)
                throw new ForbiddenException("Only team captain can submit the solution");

            ValidateTeamSize(team, task);
            ValidateDeadline(task);

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            var solution = await _context.TeamSolutions
                .Include(s => s.FileTeamSolutions)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);

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
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
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
                solution.UpdatedDate = DateTime.UtcNow;

                _context.FileTeamSolutions.RemoveRange(solution.FileTeamSolutions);
                solution.FileTeamSolutions.Clear();
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

            await _context.SaveChangesAsync();

            return new IdRequestDto { Id = solution.Id };
        }

        public async Task<IdRequestDto> DeleteSolutionAsync(Guid currentUserId, Guid taskId)
        {
            var task = await _context.TeamAssignments.FindAsync(taskId);
            if (task == null)
                throw new NotFoundException("Team assignment not found");

            var (team, isCaptain) = await GetTeamAndCaptainStatusAsync(currentUserId, task.CourseId);
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

            var team = await GetTeamForStudent(currentUserId, task.CourseId);
            if (team == null)
                throw new NotFoundException("You are not in a team");

            var solution = await _context.TeamSolutions
                .Include(s => s.FileTeamSolutions).ThenInclude(f => f.File)
                .Include(s => s.Team).ThenInclude(t => t.Members).ThenInclude(m => m.User)
                .Include(s => s.SubmittedByUser)
                .FirstOrDefaultAsync(s => s.TaskId == taskId && s.TeamId == team.Id);

            if (solution == null)
                throw new NotFoundException("Solution not found");

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
                }).ToList()
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
                .Include(s => s.Task)
                .FirstOrDefaultAsync(s => s.Id == solutionId);
            if (solution == null)
                throw new NotFoundException("Solution not found");

            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(r => r.CourseId == solution.Task.CourseId && r.UserId == currentUserId);
            if (role == null || role.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can review solutions");

            if (dto.Score.HasValue)
            {
                if (dto.Score > solution.Task.MaxScore)
                    throw new BadRequestException("Score exceeds max score");
                solution.Score = (uint)dto.Score.Value;
            }

            if (dto.Score.HasValue && solution.Score != dto.Score.Value)
            {
                await _gradeDistributionService.ResetDistributionAsync(solution.TeamId, solution.TaskId);
            }

            solution.Status = dto.Status;
            solution.UpdatedDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = solution.Id };
        }

        #region Private Helpers

        private async Task<(Team? team, bool isCaptain)> GetTeamAndCaptainStatusAsync(Guid userId, Guid courseId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.CourseId == courseId && t.Members.Any(m => m.UserId == userId));
            if (team == null) return (null, false);
            var isCaptain = team.Members.Any(m => m.UserId == userId && m.Role == TeamMemberRole.Leader);
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