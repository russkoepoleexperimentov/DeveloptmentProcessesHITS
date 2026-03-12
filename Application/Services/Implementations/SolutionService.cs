using Application.DTOs.Post;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
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

    public SolutionService(
        GcDbContext context,
        UserManager<User> userManager,
        IMapper mapper,
        IValidator<SubmitSolutionRequestDto> submitValidator,
        IValidator<UpdateSolutionRequestDto> updateValidator)
    {
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _submitValidator = submitValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IdRequestDto> SubmitSolutionAsync(Guid currentUserId, Guid taskId, SubmitSolutionRequestDto dto)
    {
        await _submitValidator.ValidateAndThrowAsync(dto);

        var task = await _context.Assignments.FindAsync(taskId);
        if (task == null)
            throw new NotFoundException("Task not found");

        var role = await _context.CourseRoles
            .FirstOrDefaultAsync(r => r.CourseId == task.CourseId && r.UserId == currentUserId);

        if (role == null || role.RoleType != UserRoleType.Student)
            throw new ForbiddenException("Only students can submit solutions");

        var solution = await _context.Solutions
            .Include(s => s.FileSolutions)
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);

        if (solution == null)
        {
            solution = new Solution
            {
                Id = Guid.NewGuid(),
                TaskId = taskId,
                UserId = currentUserId,
                Text = dto.Text ?? "",
                Status = SolutionStatus.Pending,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Solutions.Add(solution);
        }
        else
        {
            solution.Text = dto.Text ?? "";
            solution.Status = SolutionStatus.Pending;
            solution.UpdatedDate = DateTime.UtcNow;

            solution.FileSolutions.Clear();
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
            .Include(s => s.FileSolutions)
            .FirstOrDefaultAsync(s => s.TaskId == taskId && s.UserId == currentUserId);

        if (solution == null)
            throw new NotFoundException("Solution not found");

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
            UpdatedDate = solution.UpdatedDate
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
}