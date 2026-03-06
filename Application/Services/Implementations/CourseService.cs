using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations
{
    public class CourseService : ICourseService
    {
        private readonly GcDbContext _context;
        private readonly UserManager<User> _userManager;

        public CourseService(GcDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<CreateUpdateCourseResponseDto> CreateCourseAsync(Guid userId, CreateUpdateCourseRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            if (user == null)
                throw new NotFoundException("User not found");

            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = request.Title,
                AuthorId = userId,
                InviteCode = GenerateInviteCode(),
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Courses.Add(course);

            var courseRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = userId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.CourseRoles.Add(courseRole);

            await _context.SaveChangesAsync();

            return new CreateUpdateCourseResponseDto
            {
                Id = course.Id,
                Title = course.Title
            };
        }

        public async Task<JoinCourseResponseDto> JoinCourseAsync(Guid userId, JoinCourseRequestDto request)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user == null)
                throw new NotFoundException("User not found");

            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.InviteCode == request.InviteCode);

            if (course == null)
                throw new NotFoundException("Course not found");

            var existingRole = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == course.Id && cr.UserId == userId);

            if (existingRole)
                throw new EntryExistsException("User is already a member of this course");

            var courseRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = userId,
                RoleType = UserRoleType.Student,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.CourseRoles.Add(courseRole);
            await _context.SaveChangesAsync();

            return new JoinCourseResponseDto
            {
                Id = course.Id,
                Title = course.Title,
                Role = UserRoleType.Student
            };
        }

        public async Task<CourseDetailsDto> GetCourseDetailsAsync(Guid userId, Guid courseId)
        {
            var course = await _context.Courses
                .FirstOrDefaultAsync(c => c.Id == courseId);

            if (course == null)
                throw new NotFoundException("Course not found");

            var userRole = await _context.CourseRoles
                .Where(cr => cr.CourseId == courseId && cr.UserId == userId)
                .Select(cr => cr.RoleType)
                .FirstOrDefaultAsync();

            if (userRole == 0) // Student is 0, so we need to check if any role exists
            {
                var hasRole = await _context.CourseRoles
                    .AnyAsync(cr => cr.CourseId == courseId && cr.UserId == userId);
                if (!hasRole)
                    throw new ForbiddenException("User is not a member of this course");
            }

            return new CourseDetailsDto
            {
                Id = course.Id,
                Title = course.Title,
                AuthorId = course.AuthorId,
                InviteCode = course.InviteCode,
                Role = userRole
            };
        }

        public async Task<ChangeRoleResponseDto> ChangeRoleAsync(Guid currentUserId, Guid courseId, Guid targetUserId, ChangeRoleRequestDto request)
        {
            var currentUserRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);

            if (currentUserRole == null || currentUserRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can change roles");

            if (currentUserId == targetUserId)
                throw new BadRequestException("Cannot change your own role");

            var targetRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == targetUserId);

            if (targetRole == null)
                throw new NotFoundException("Target user is not a member of this course");

            var course = await _context.Courses.FindAsync(courseId);
            if (course.AuthorId == targetUserId)
                throw new BadRequestException("Cannot change role of course creator");

            targetRole.RoleType = request.Role;
            targetRole.UpdatedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new ChangeRoleResponseDto
            {
                Id = targetUserId,
                Role = targetRole.RoleType
            };
        }

        public async Task RemoveMemberAsync(Guid currentUserId, Guid courseId, Guid targetUserId)
        {
            var currentUserRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);

            if (currentUserRole == null || currentUserRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can remove members");

            if (currentUserId == targetUserId)
                throw new BadRequestException("Cannot remove yourself from the course");

            var targetRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == targetUserId);

            if (targetRole == null)
                throw new NotFoundException("Target user is not a member of this course");

            var course = await _context.Courses.FindAsync(courseId);
            if (course.AuthorId == targetUserId)
                throw new BadRequestException("Cannot remove course creator");

            _context.CourseRoles.Remove(targetRole);
            await _context.SaveChangesAsync();
        }

        public async Task<PagedResponse<CourseMemberDto>> GetMembersAsync(Guid currentUserId, Guid courseId, int skip, int take, string? query)
        {
            var currentUserRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);

            if (currentUserRole == null || currentUserRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can view members list");

            var membersQuery = from cr in _context.CourseRoles
                               join u in _context.Users on cr.UserId equals u.Id
                               where cr.CourseId == courseId
                               select new CourseMemberDto
                               {
                                   Id = u.Id,
                                   Credentials = u.Credentials,
                                   Email = u.Email,
                                   Role = cr.RoleType
                               };

            if (!string.IsNullOrWhiteSpace(query))
            {
                membersQuery = membersQuery.Where(m => m.Credentials.Contains(query));
            }

            var totalRecords = await membersQuery.CountAsync();

            var records = await membersQuery
                .OrderBy(m => m.Credentials)
                .Skip(skip)
                .Take(take)
                .ToListAsync();

            return new PagedResponse<CourseMemberDto>
            {
                Records = records,
                TotalRecords = totalRecords
            };
        }

        private string GenerateInviteCode()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
        }
    }
}
