using Application.Services.Abstractions;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
using Domain.Models.Domain.Models;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations
{
    public class TeamManagerService : ITeamManagerService
    {
        private readonly GcDbContext _context;
        private readonly ICaptainStrategyFactory _strategyFactory;

        public TeamManagerService(GcDbContext context, ICaptainStrategyFactory strategyFactory)
        {
            _context = context;
            _strategyFactory = strategyFactory;
        }
        private async Task<bool> IsTeacherOfCourseAsync(Guid courseId, Guid userId)
        {
            var role = await _context.CourseRoles.FirstOrDefaultAsync(r => r.CourseId == courseId && r.UserId == userId);
            return role != null && role.RoleType == UserRoleType.Teacher;
        }

        private async Task<bool> IsTeacherOfTeamAsync(Guid teamId, Guid userId)
        {
            var team = await _context.Teams.Include(t => t.Assignment).FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return false;
            return await IsTeacherOfCourseAsync(team.CourseId, userId);
        }

        private async Task<bool> IsTeacherOfAssignmentAsync(Guid assignmentId, Guid userId)
        {
            var assignment = await _context.TeamAssignments.FindAsync(assignmentId);
            if (assignment == null) return false;
            return await IsTeacherOfCourseAsync(assignment.CourseId, userId);
        }

        private async Task<bool> IsInAnyTeamForAssignmentAsync(Guid userId, Guid assignmentId)
        {
            return await _context.TeamMembers
                .AnyAsync(m => m.UserId == userId && m.Team.AssignmentId == assignmentId);
        }

        private async Task<bool> IsMemberOfTeamAsync(Guid teamId, Guid userId)
        {
            return await _context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
        }

        public async Task JoinTeamAsync(Guid teamId, Guid studentId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment == null) throw new NotFoundException("Assignment not found");

            if (!assignment.AllowJoinTeam)
                throw new ForbiddenException("Joining team is not allowed for this assignment");
            if (team.Members.Count >= assignment.MaxTeamSize)
                throw new BadRequestException("Team is full");
            if (await IsInAnyTeamForAssignmentAsync(studentId, assignment.Id))
                throw new BadRequestException("You are already in a team for this assignment");

            await AddMemberInternalAsync(teamId, studentId, isLeader: false);
            await _context.SaveChangesAsync();

            if (team.Members.Count == 1)
                await ReassignCaptainByStrategyAsync(teamId, assignment);
        }

        public async Task LeaveTeamAsync(Guid teamId, Guid studentId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment == null) throw new NotFoundException("Assignment not found");

            if (!assignment.AllowLeaveTeam)
                throw new ForbiddenException("Leaving team is not allowed for this assignment");

            var member = team.Members.FirstOrDefault(m => m.UserId == studentId);
            if (member == null) throw new NotFoundException("You are not in this team");

            bool wasLeader = member.Role == TeamMemberRole.Leader;
            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            if (wasLeader && team.Members.Any())
                await ReassignCaptainByStrategyAsync(teamId, assignment);
        }

        public async Task TransferCaptainAsync(Guid teamId, Guid toStudentId, Guid currentStudentId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment == null) throw new NotFoundException("Assignment not found");

            var strategy = _strategyFactory.GetStrategy(assignment.CaptainMode);
            if (!strategy.CanStudentTransfer || !assignment.AllowStudentTransferCaptain)
                throw new ForbiddenException("Captain transfer is not allowed for this assignment");

            var fromMember = team.Members.FirstOrDefault(m => m.UserId == currentStudentId);
            if (fromMember == null || fromMember.Role != TeamMemberRole.Leader)
                throw new ForbiddenException("Only current captain can transfer");

            var toMember = team.Members.FirstOrDefault(m => m.UserId == toStudentId);
            if (toMember == null) throw new BadRequestException("Target student is not a member of this team");

            fromMember.Role = TeamMemberRole.Member;
            toMember.Role = TeamMemberRole.Leader;
            await _context.SaveChangesAsync();
        }

        public async Task AddStudentToTeamAsync(Guid teamId, Guid studentId, Guid teacherId)
        {
            if (!await IsTeacherOfTeamAsync(teamId, teacherId))
                throw new ForbiddenException("Only teacher can add students");

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment == null) throw new NotFoundException("Assignment not found");

            if (team.Members.Count >= assignment.MaxTeamSize)
                throw new BadRequestException("Team is full");
            if (await IsInAnyTeamForAssignmentAsync(studentId, assignment.Id))
                throw new BadRequestException("Student is already in a team for this assignment");

            await AddMemberInternalAsync(teamId, studentId, isLeader: false);
            await _context.SaveChangesAsync();

            if (team.Members.Count == 1)
                await ReassignCaptainByStrategyAsync(teamId, assignment);
        }

        public async Task RemoveStudentFromTeamAsync(Guid teamId, Guid studentId, Guid teacherId)
        {
            if (!await IsTeacherOfTeamAsync(teamId, teacherId))
                throw new ForbiddenException("Only teacher can remove students");

            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var member = team.Members.FirstOrDefault(m => m.UserId == studentId);
            if (member == null) throw new NotFoundException("Student not in this team");

            bool wasLeader = member.Role == TeamMemberRole.Leader;
            _context.TeamMembers.Remove(member);
            await _context.SaveChangesAsync();

            if (wasLeader && team.Members.Any())
            {
                var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
                if (assignment != null)
                    await ReassignCaptainByStrategyAsync(teamId, assignment);
            }
        }

        public async Task RenameTeamAsync(Guid teamId, string newName, Guid teacherId)
        {
            if (!await IsTeacherOfTeamAsync(teamId, teacherId))
                throw new ForbiddenException("Only teacher can rename team");

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) throw new NotFoundException("Team not found");
            team.Name = newName;
            await _context.SaveChangesAsync();
        }

        public async Task<List<TeamDto>> GetTeamsForAssignmentAsync(Guid assignmentId, Guid teacherId)
        {
            if (!await IsTeacherOfAssignmentAsync(assignmentId, teacherId))
                throw new ForbiddenException("Only teacher can view all teams");

            var teams = await _context.Teams
                .Include(t => t.Members).ThenInclude(m => m.User)
                .Where(t => t.AssignmentId == assignmentId)
                .ToListAsync();
            return teams.Select(MapToTeamDto).ToList();
        }

        public async Task<TeamDto?> GetMyTeamForAssignmentAsync(Guid assignmentId, Guid studentId)
        {
            var team = await _context.Teams
                .Include(t => t.Members).ThenInclude(m => m.User)
                .FirstOrDefaultAsync(t => t.AssignmentId == assignmentId && t.Members.Any(m => m.UserId == studentId));
            return team == null ? null : MapToTeamDto(team);
        }

        public async Task<bool> IsCaptainAsync(Guid teamId, Guid userId)
        {
            return await _context.TeamMembers
                .AnyAsync(m => m.TeamId == teamId && m.UserId == userId && m.Role == TeamMemberRole.Leader);
        }

        public async Task StartVotingAsync(Guid teamId, Guid initiatorId)
        {
            if (!await IsMemberOfTeamAsync(teamId, initiatorId))
                throw new ForbiddenException("You are not a member of this team");

            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) throw new NotFoundException("Team not found");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment?.CaptainMode != CaptainSelectionMode.VotingAndLottery)
                throw new BadRequestException("Voting is not enabled for this assignment");

            var oldVotes = await _context.CaptainVotes.Where(v => v.TeamId == teamId).ToListAsync();
            _context.CaptainVotes.RemoveRange(oldVotes);
            await _context.SaveChangesAsync();
        }

        public async Task SetFixedCaptainAsync(Guid teamId, Guid? studentId, Guid teacherId)
        {
            if (!await IsTeacherOfTeamAsync(teamId, teacherId))
                throw new ForbiddenException("Only teacher can set fixed captain");
            var team = await _context.Teams.FindAsync(teamId);
            if (team == null) throw new NotFoundException("Team not found");
            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment?.CaptainMode != CaptainSelectionMode.TeacherFixed)
                throw new BadRequestException("Captain mode is not TeacherFixed");
            team.FixedCaptainId = studentId;
            await _context.SaveChangesAsync();
            await ReassignCaptainByStrategyAsync(teamId, assignment);
        }
        public async Task CastVoteAsync(Guid teamId, Guid candidateId, Guid voterId)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) throw new NotFoundException("Team not found");

            if (!team.Members.Any(m => m.UserId == voterId))
                throw new ForbiddenException("You are not a member of this team");
            if (!team.Members.Any(m => m.UserId == candidateId))
                throw new BadRequestException("Candidate is not a member of this team");

            var assignment = await _context.TeamAssignments.FindAsync(team.AssignmentId);
            if (assignment?.CaptainMode != CaptainSelectionMode.VotingAndLottery)
                throw new BadRequestException("Voting is not enabled for this assignment");

            var firstVote = await _context.CaptainVotes
                .Where(v => v.TeamId == teamId)
                .OrderBy(v => v.VotedAt)
                .FirstOrDefaultAsync();
            if (firstVote != null && assignment.VotingDurationHours.HasValue && DateTime.UtcNow > firstVote.VotedAt.AddHours(assignment.VotingDurationHours.Value))
                throw new BadRequestException("Voting period has ended");

            var existing = await _context.CaptainVotes
                .FirstOrDefaultAsync(v => v.TeamId == teamId && v.VoterId == voterId);
            if (existing != null)
                _context.CaptainVotes.Remove(existing);

            _context.CaptainVotes.Add(new CaptainVote
            {
                Id = Guid.NewGuid(),
                TeamId = teamId,
                VoterId = voterId,
                CandidateId = candidateId,
                VotedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            var votesCount = await _context.CaptainVotes.CountAsync(v => v.TeamId == teamId);
            if (votesCount >= team.Members.Count)
                await ReassignCaptainByStrategyAsync(teamId, assignment);
        }

        private async Task AddMemberInternalAsync(Guid teamId, Guid userId, bool isLeader)
        {
            _context.TeamMembers.Add(new TeamMember
            {
                TeamId = teamId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow,
                Role = isLeader ? TeamMemberRole.Leader : TeamMemberRole.Member
            });
            await Task.CompletedTask;
        }

        public async Task<List<TeamDto>> GetTeamsForStudentAsync(Guid assignmentId, Guid studentId)
        {
            var assignment = await _context.TeamAssignments.FindAsync(assignmentId);
            if (assignment == null) throw new NotFoundException("Assignment not found");

            var isMember = await _context.CourseRoles
                .AnyAsync(r => r.CourseId == assignment.CourseId && r.UserId == studentId);
            if (!isMember) throw new ForbiddenException("You are not a member of this course");

            var teams = await _context.Teams
                .Include(t => t.Members).ThenInclude(m => m.User)
                .Where(t => t.AssignmentId == assignmentId)
                .ToListAsync();

            return teams.Select(MapToTeamDto).ToList();
        }

        private async Task ReassignCaptainByStrategyAsync(Guid teamId, TeamAssignment assignment)
        {
            var team = await _context.Teams
                .Include(t => t.Members)
                .FirstOrDefaultAsync(t => t.Id == teamId);
            if (team == null) return;

            var strategy = _strategyFactory.GetStrategy(assignment.CaptainMode);
            var newCaptainId = await strategy.DetermineCaptainAsync(team, assignment);

            foreach (var member in team.Members)
                member.Role = TeamMemberRole.Member;

            if (newCaptainId.HasValue)
            {
                var newCaptain = team.Members.FirstOrDefault(m => m.UserId == newCaptainId.Value);
                if (newCaptain != null)
                    newCaptain.Role = TeamMemberRole.Leader;
            }
            await _context.SaveChangesAsync();
        }

        private TeamDto MapToTeamDto(Team team) => new()
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
}