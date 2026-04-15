using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
using GoogleClass.DTOs.GradeDistribution;
using GoogleClass.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations;

public class GradeDistributionService : IGradeDistributionService
{
    private readonly GcDbContext _context;

    public GradeDistributionService(GcDbContext context)
    {
        _context = context;
    }

    public async Task<GradeDistributionResponseDto> GetDistributionAsync(Guid teamId, Guid assignmentId, Guid currentUserId)
    {
        // Проверка, что пользователь – участник команды или преподаватель курса
        await EnsureAccessAsync(teamId, assignmentId, currentUserId, allowTeacher: true);

        var distribution = await LoadDistributionAsync(teamId, assignmentId);
        if (distribution == null)
        {
            // Создаём дефолтное (равное) распределение
            distribution = await CreateDefaultDistributionAsync(teamId, assignmentId);
        }

        return MapToResponse(distribution);
    }

    public async Task<GradeDistributionResponseDto> UpdateDistributionAsync(Guid teamId, Guid assignmentId, Guid currentUserId, GradeDistributionUpdateRequestDto dto)
    {
        // Только капитан команды
        await EnsureCaptainAsync(teamId, assignmentId, currentUserId);

        var distribution = await LoadDistributionAsync(teamId, assignmentId);
        if (distribution == null)
        {
            distribution = await CreateDefaultDistributionAsync(teamId, assignmentId);
        }

        // Валидация: все userId должны быть членами команды
        var teamMembers = await _context.TeamMembers
            .Where(m => m.TeamId == teamId)
            .Select(m => m.UserId)
            .ToListAsync();

        foreach (var entry in dto.Entries)
        {
            if (!teamMembers.Contains(entry.UserId))
                throw new BadRequestException($"User {entry.UserId} is not a member of this team");
            if (entry.Points < 0)
                throw new BadRequestException("Points cannot be negative");
        }

        decimal sum = dto.Entries.Sum(e => e.Points);
        if (sum > distribution.RawScore)
            throw new BadRequestException($"Sum of distributed points ({sum}) exceeds team raw score ({distribution.RawScore})");

        // Обновляем записи
        var existingEntries = await _context.GradeDistributionEntries
            .Where(e => e.DistributionId == distribution.Id)
            .ToListAsync();

        _context.GradeDistributionEntries.RemoveRange(existingEntries);
        foreach (var entry in dto.Entries)
        {
            _context.GradeDistributionEntries.Add(new GradeDistributionEntry
            {
                Id = Guid.NewGuid(),
                DistributionId = distribution.Id,
                UserId = entry.UserId,
                Points = entry.Points
            });
        }

        distribution.IsCustomized = true;
        distribution.UpdatedDate = DateTime.UtcNow;

        // При изменении распределения сбрасываем голоса
        var oldVotes = await _context.GradeDistributionVotes
            .Where(v => v.DistributionId == distribution.Id)
            .ToListAsync();
        _context.GradeDistributionVotes.RemoveRange(oldVotes);

        await _context.SaveChangesAsync();

        return MapToResponse(distribution);
    }

    public async Task CastVoteAsync(Guid teamId, Guid assignmentId, Guid currentUserId, GradeDistributionVoteRequestDto dto)
    {
        // Участник команды (не капитан? по спецификации – любой участник)
        await EnsureAccessAsync(teamId, assignmentId, currentUserId, allowTeacher: false);

        var distribution = await LoadDistributionAsync(teamId, assignmentId);
        if (distribution == null)
            throw new BadRequestException("No distribution found for this team/assignment. Please try again later.");

        // Замена существующего голоса
        var existingVote = await _context.GradeDistributionVotes
            .FirstOrDefaultAsync(v => v.DistributionId == distribution.Id && v.UserId == currentUserId);

        if (existingVote != null)
            _context.GradeDistributionVotes.Remove(existingVote);

        _context.GradeDistributionVotes.Add(new GradeDistributionVote
        {
            Id = Guid.NewGuid(),
            DistributionId = distribution.Id,
            UserId = currentUserId,
            Vote = dto.Vote
        });

        await _context.SaveChangesAsync();
    }

    public async Task ResetDistributionAsync(Guid teamId, Guid assignmentId)
    {
        var distribution = await LoadDistributionAsync(teamId, assignmentId);
        if (distribution == null) return;

        // Удаляем старые entry и votes
        var entries = _context.GradeDistributionEntries.Where(e => e.DistributionId == distribution.Id);
        var votes = _context.GradeDistributionVotes.Where(v => v.DistributionId == distribution.Id);
        _context.GradeDistributionEntries.RemoveRange(entries);
        _context.GradeDistributionVotes.RemoveRange(votes);

        // Пересоздаём равное распределение
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == teamId)
            .Select(m => m.UserId)
            .ToListAsync();

        var rawScore = distribution.RawScore;
        var memberCount = members.Count;
        var defaultPoints = memberCount > 0 ? (decimal)rawScore / memberCount : 0;

        foreach (var userId in members)
        {
            _context.GradeDistributionEntries.Add(new GradeDistributionEntry
            {
                Id = Guid.NewGuid(),
                DistributionId = distribution.Id,
                UserId = userId,
                Points = defaultPoints
            });
        }

        distribution.IsCustomized = false;
        distribution.UpdatedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // Вспомогательные методы

    private async Task<GradeDistribution?> LoadDistributionAsync(Guid teamId, Guid assignmentId)
    {
        return await _context.GradeDistributions
            .Include(d => d.Entries)
            .FirstOrDefaultAsync(d => d.TeamId == teamId && d.AssignmentId == assignmentId);
    }

    private async Task<GradeDistribution> CreateDefaultDistributionAsync(Guid teamId, Guid assignmentId)
    {
        // Получаем raw-оценку из TeamSolution
        var teamSolution = await _context.TeamSolutions
            .FirstOrDefaultAsync(ts => ts.TeamId == teamId && ts.TaskId == assignmentId);
        if (teamSolution == null)
            throw new NotFoundException("Team solution not found for this assignment");

        var rawScore = teamSolution.Score;

        var distribution = new GradeDistribution
        {
            Id = Guid.NewGuid(),
            TeamId = teamId,
            AssignmentId = assignmentId,
            RawScore = rawScore,
            IsCustomized = false,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        _context.GradeDistributions.Add(distribution);
        await _context.SaveChangesAsync();

        // Заполняем равными баллами
        var members = await _context.TeamMembers
            .Where(m => m.TeamId == teamId)
            .Select(m => m.UserId)
            .ToListAsync();

        var memberCount = members.Count;
        var defaultPoints = memberCount > 0 ? (decimal)rawScore / memberCount : 0;

        foreach (var userId in members)
        {
            _context.GradeDistributionEntries.Add(new GradeDistributionEntry
            {
                Id = Guid.NewGuid(),
                DistributionId = distribution.Id,
                UserId = userId,
                Points = defaultPoints
            });
        }

        await _context.SaveChangesAsync();
        return distribution;
    }

    private async Task EnsureAccessAsync(Guid teamId, Guid assignmentId, Guid userId, bool allowTeacher)
    {
        var isMember = await _context.TeamMembers.AnyAsync(m => m.TeamId == teamId && m.UserId == userId);
        if (isMember) return;

        if (allowTeacher)
        {
            var team = await _context.Teams.FindAsync(teamId);
            var isTeacher = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == team.CourseId && cr.UserId == userId && cr.RoleType == UserRoleType.Teacher);
            if (isTeacher) return;
        }

        throw new ForbiddenException("You do not have access to this grade distribution");
    }

    private async Task EnsureCaptainAsync(Guid teamId, Guid assignmentId, Guid userId)
    {
        var team = await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);
        if (team == null) throw new NotFoundException("Team not found");

        var isCaptain = team.FixedCaptainId == userId
            || team.Members.Any(m => m.UserId == userId && m.Role == TeamMemberRole.Leader);
        if (!isCaptain)
            throw new ForbiddenException("Only the team captain can modify grade distribution");
    }

    private GradeDistributionResponseDto MapToResponse(GradeDistribution distribution)
    {
        var entries = distribution.Entries.Select(e => new GradeDistributionEntryDto
        {
            UserId = e.UserId,
            Points = e.Points
        }).ToList();

        return new GradeDistributionResponseDto
        {
            TeamId = distribution.TeamId,
            AssignmentId = distribution.AssignmentId,
            TeamRawScore = distribution.RawScore,
            Entries = entries,
            SumDistributed = entries.Sum(e => e.Points),
            DistributionChanged = distribution.IsCustomized
        };
    }
}