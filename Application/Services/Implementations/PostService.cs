using Application.DTOs.Post;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Domain.Models;
using FluentValidation;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.DTOs.Post;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Implementations
{
    public class PostService : IPostService
    {
        private readonly GcDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateUpdatePostDto> _validator;

        public PostService(
            GcDbContext context,
            UserManager<User> userManager,
            IMapper mapper,
            IValidator<CreateUpdatePostDto> validator)
        {
            _context = context;
            _userManager = userManager;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<IdRequestDto> CreatePostAsync(Guid currentUserId, Guid courseId, CreateUpdatePostDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var course = await _context.Courses.FindAsync(courseId);
            if (course == null)
                throw new NotFoundException("Course not found");

            var userRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can create posts in this course");

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            GenericPost post;
            if (dto.Type == PostType.POST)
            {
                post = _mapper.Map<RegularPost>(dto);
                post.Id = Guid.NewGuid();
                post.CourseId = courseId;
                post.AuthorId = currentUserId;
                post.CreatedDate = DateTime.UtcNow;
                post.UpdatedDate = DateTime.UtcNow;

                _context.Posts.Add((RegularPost)post);
            }
            else
            {
                var assignment = _mapper.Map<Assignment>(dto);
                assignment.Id = Guid.NewGuid();
                assignment.CourseId = courseId;
                assignment.AuthorId = currentUserId;
                assignment.CreatedDate = DateTime.UtcNow;
                assignment.UpdatedDate = DateTime.UtcNow;
                assignment.TaskType = dto.TaskType.Value;

                _context.Assignments.Add(assignment);
                post = assignment;
            }

            await _context.SaveChangesAsync();

            if (dto.Files != null && dto.Files.Any())
            {
                var filePosts = dto.Files.Select(fileId => new FilePost
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    FileId = fileId
                });
                _context.FilePosts.AddRange(filePosts);
                await _context.SaveChangesAsync();
            }

            return new IdRequestDto { Id = post.Id };
        }

        public async Task<PostDetailsDto> GetPostAsync(Guid currentUserId, Guid postId)
        {
            var post = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.FilePosts) 
                    .ThenInclude(fp => fp.File)
                .FirstOrDefaultAsync(p => p.Id == postId) as GenericPost;

            if (post == null)
            {
                post = await _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.FilePosts)
                        .ThenInclude(fp => fp.File)
                    .FirstOrDefaultAsync(a => a.Id == postId);
            }

            if (post == null)
                throw new NotFoundException("Post not found");

            var isMember = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == post.CourseId && cr.UserId == currentUserId);
            if (!isMember)
                throw new ForbiddenException("You are not a member of this course");

            var response = new PostDetailsDto
            {
                Id = post.Id,
                Type = post is Assignment ? PostType.TASK : PostType.POST,
                Title = post.Title,
                Text = post.Text,
                UserSolution = null,
                Files = post.FilePosts?.Select(fp => new FileDto
                {
                    Id = fp.FileId.ToString(),
                    Name = fp.File.OriginalName
                }).ToList()
            };

            if (post is Assignment assignment)
            {
                response.Deadline = assignment.Deadline;
                response.MaxScore = (int?)assignment.MaxScore;
                response.SolvableAfterDeadline = assignment.SolvableAfterDeadline;
                response.TaskType = assignment.TaskType;
            }

            return response;
        }

        public async Task<IdRequestDto> UpdatePostAsync(Guid currentUserId, Guid postId, CreateUpdatePostDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);

            var post = await _context.Posts
                .Include(p => p.Course)
                .Include(p => p.FilePosts)
                .FirstOrDefaultAsync(p => p.Id == postId) as GenericPost;

            if (post == null)
            {
                post = await _context.Assignments
                    .Include(a => a.Course)
                    .Include(a => a.FilePosts)
                    .FirstOrDefaultAsync(a => a.Id == postId);
            }

            if (post == null)
                throw new NotFoundException("Post not found");

            var userRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == post.CourseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can update posts");

            if ((dto.Type == PostType.POST && post is Assignment) ||
                (dto.Type == PostType.TASK && post is not Assignment))
            {
                throw new BadRequestException("Post type mismatch");
            }

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            post.Title = dto.Title;
            post.Text = dto.Text;
            post.UpdatedDate = DateTime.UtcNow;

            if (post is Assignment assignment && dto.Type == PostType.TASK)
            {
                assignment.Deadline = dto.Deadline;
                assignment.MaxScore = (uint)(dto.MaxScore ?? 5);
                assignment.SolvableAfterDeadline = dto.SolvableAfterDeadline ?? false;
                assignment.TaskType = dto.TaskType.Value;
            }

            if (post.FilePosts != null && post.FilePosts.Any())
            {
                _context.FilePosts.RemoveRange(post.FilePosts);
            }

            if (dto.Files != null && dto.Files.Any())
            {
                var newFilePosts = dto.Files.Select(fileId => new FilePost
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    FileId = fileId
                });
                await _context.FilePosts.AddRangeAsync(newFilePosts);
            }

            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = post.Id };
        }

        public async Task<IdRequestDto> DeletePostAsync(Guid currentUserId, Guid postId)
        {
            var post = await _context.Posts
                .Include(p => p.FilePosts)
                .FirstOrDefaultAsync(p => p.Id == postId) as GenericPost;

            if (post == null)
            {
                post = await _context.Assignments
                    .Include(a => a.FilePosts)
                    .FirstOrDefaultAsync(a => a.Id == postId);
            }

            if (post == null)
                throw new NotFoundException("Post not found");

            var courseId = post.CourseId;
            var userRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can delete posts");

            if (post.FilePosts != null && post.FilePosts.Any())
            {
                _context.FilePosts.RemoveRange(post.FilePosts);
            }

            _context.Remove(post);
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = post.Id };
        }

        public async Task<FeedResponseDto> GetCourseFeedAsync(Guid currentUserId, Guid courseId, int skip, int take)
        {
            var isMember = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);
            if (!isMember)
                throw new ForbiddenException("You are not a member of this course");

            var postsQuery = _context.Posts
                .Where(p => p.CourseId == courseId)
                .Select(p => new { p.Id, p.Title, p.CreatedDate, Type = "post" });

            var assignmentsQuery = _context.Assignments
                .Where(a => a.CourseId == courseId)
                .Select(a => new { a.Id, a.Title, a.CreatedDate, Type = "task" });

            var union = postsQuery.Union(assignmentsQuery)
                .OrderByDescending(x => x.CreatedDate);

            var totalRecords = await union.CountAsync();

            var records = await union
                .Skip(skip)
                .Take(take)
                .Select(x => new CourseFeedItemDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    CreatedDate = x.CreatedDate,
                    Type = x.Type == "post" ? PostType.POST : PostType.TASK
                })
                .ToListAsync();

            return new FeedResponseDto
            {
                Records = records,
                TotalRecords = totalRecords
            };
        }

        private async Task ValidateFilesExist(IEnumerable<Guid> fileIds)
        {
            var existing = await _context.UserFiles.CountAsync(f => fileIds.Contains(f.Id));
            if (existing != fileIds.Count())
                throw new NotFoundException("One or more files not found");
        }
    }
}