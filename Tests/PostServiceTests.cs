using Application.DTOs.Post;
using Application.Services.Implementations;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Domain.Models;
using FluentAssertions;
using FluentValidation;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Post;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests
{
    public class PostServiceTests
    {
        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<IValidator<CreateUpdatePostDto>> _validator;

        private readonly GcDbContext _context;
        private readonly PostService _service;

        public PostServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _userManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mapper = new Mock<IMapper>();
            _validator = new Mock<IValidator<CreateUpdatePostDto>>();

            var options = new DbContextOptionsBuilder<GcDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new GcDbContext(options);

            _service = new PostService(
                _context,
                _userManager.Object,
                _mapper.Object,
                _validator.Object);
        }

        private async Task<(Guid userId, Guid courseId)> SeedTeacherAndCourse()
        {
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                Title = "Test Course",
                AuthorId = userId,
                InviteCode = "TEST123"
            };
            _context.Courses.Add(course);

            var courseRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = userId,
                RoleType = UserRoleType.Teacher
            };
            _context.CourseRoles.Add(courseRole);

            await _context.SaveChangesAsync();
            return (userId, courseId);
        }

        private async Task<Guid> SeedStudent(Guid courseId)
        {
            var userId = Guid.NewGuid();
            var courseRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = userId,
                RoleType = UserRoleType.Student
            };
            _context.CourseRoles.Add(courseRole);
            await _context.SaveChangesAsync();
            return userId;
        }

        #region CreatePostAsync Tests

        [Fact]
        public async Task CreatePostAsync_ShouldThrow_WhenCourseNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new CreateUpdatePostDto { Type = PostType.POST };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreatePostAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreatePostAsync_ShouldThrow_WhenUserNotTeacher()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);
            var dto = new CreateUpdatePostDto { Type = PostType.POST };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.CreatePostAsync(studentId, courseId, dto));
        }

        [Fact]
        public async Task CreatePostAsync_ShouldCreateRegularPost_WhenTypePost()
        {
            var (userId, courseId) = await SeedTeacherAndCourse();
            var dto = new CreateUpdatePostDto
            {
                Type = PostType.POST,
                Title = "Test Post",
                Text = "Content"
            };

            _validator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var regularPost = new RegularPost
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Text = dto.Text
            };
            _mapper.Setup(m => m.Map<RegularPost>(dto)).Returns(regularPost);

            var result = await _service.CreatePostAsync(userId, courseId, dto);

            result.Id.Should().NotBeEmpty();

            var savedPost = await _context.Posts.FindAsync(result.Id);
            savedPost.Should().NotBeNull();
            savedPost.Title.Should().Be(dto.Title);
            savedPost.Text.Should().Be(dto.Text);
            savedPost.CourseId.Should().Be(courseId);
            savedPost.AuthorId.Should().Be(userId);
        }

        [Fact]
        public async Task CreatePostAsync_ShouldCreateAssignment_WhenTypeTask()
        {
            var (userId, courseId) = await SeedTeacherAndCourse();
            var dto = new CreateUpdatePostDto
            {
                Type = PostType.TASK,
                Title = "Test Task",
                Text = "Solve this",
                Deadline = DateTime.UtcNow.AddDays(7),
                MaxScore = 10,
                TaskType = TaskType.Mandatory,
                SolvableAfterDeadline = true
            };

            _validator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                Title = dto.Title,
                Text = dto.Text,
                Deadline = dto.Deadline,
                MaxScore = (uint)dto.MaxScore.Value,
                SolvableAfterDeadline = dto.SolvableAfterDeadline.Value,
                TaskType = dto.TaskType.Value
            };
            _mapper.Setup(m => m.Map<Assignment>(dto)).Returns(assignment);

            var result = await _service.CreatePostAsync(userId, courseId, dto);

            result.Id.Should().NotBeEmpty();

            var savedAssignment = await _context.Assignments.FindAsync(result.Id);
            savedAssignment.Should().NotBeNull();
            savedAssignment.Title.Should().Be(dto.Title);
            savedAssignment.Deadline.Should().Be(dto.Deadline);
            savedAssignment.MaxScore.Should().Be((uint)dto.MaxScore);
            savedAssignment.TaskType.Should().Be(dto.TaskType.Value);
            savedAssignment.SolvableAfterDeadline.Should().Be(dto.SolvableAfterDeadline.Value);
        }

        #endregion

        #region GetPostAsync Tests

        [Fact]
        public async Task GetPostAsync_ShouldThrow_WhenPostNotFound()
        {
            var userId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetPostAsync(userId, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetPostAsync_ShouldThrow_WhenUserNotMember()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var outsiderId = Guid.NewGuid();

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Post",
                Text = "Text"
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.GetPostAsync(outsiderId, post.Id));
        }

        [Fact]
        public async Task GetPostAsync_ShouldReturnRegularPost_WhenMember()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Post",
                Text = "Text",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = await _service.GetPostAsync(studentId, post.Id);

            result.Id.Should().Be(post.Id);
            result.Type.Should().Be(PostType.POST);
            result.Title.Should().Be(post.Title);
            result.Text.Should().Be(post.Text);
            result.Deadline.Should().BeNull();
            result.MaxScore.Should().BeNull();
            result.TaskType.Should().BeNull();
        }

        [Fact]
        public async Task GetPostAsync_ShouldReturnAssignment_WhenMember()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Task",
                Text = "Solve",
                Deadline = DateTime.UtcNow.AddDays(7),
                MaxScore = 10,
                TaskType = TaskType.Mandatory,
                SolvableAfterDeadline = true,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var result = await _service.GetPostAsync(studentId, assignment.Id);

            result.Id.Should().Be(assignment.Id);
            result.Type.Should().Be(PostType.TASK);
            result.Deadline.Should().Be(assignment.Deadline);
            result.MaxScore.Should().Be((int)assignment.MaxScore);
            result.TaskType.Should().Be(assignment.TaskType);
            result.SolvableAfterDeadline.Should().Be(assignment.SolvableAfterDeadline);
        }

        #endregion

        #region UpdatePostAsync Tests

        [Fact]
        public async Task UpdatePostAsync_ShouldThrow_WhenPostNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new CreateUpdatePostDto { Type = PostType.POST };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdatePostAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrow_WhenUserNotTeacher()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Old",
                Text = "Old text"
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var dto = new CreateUpdatePostDto
            {
                Type = PostType.POST,
                Title = "New",
                Text = "New text"
            };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdatePostAsync(studentId, post.Id, dto));
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldThrow_WhenTypeMismatch()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Task",
                Text = "Solve"
            };
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var dto = new CreateUpdatePostDto
            {
                Type = PostType.POST, 
                Title = "New",
                Text = "New text"
            };

            _validator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            await Assert.ThrowsAsync<BadRequestException>(() =>
                _service.UpdatePostAsync(teacherId, assignment.Id, dto));
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdateRegularPost_WhenValid()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Old",
                Text = "Old text"
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var dto = new CreateUpdatePostDto
            {
                Type = PostType.POST,
                Title = "New",
                Text = "New text"
            };

            _validator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var result = await _service.UpdatePostAsync(teacherId, post.Id, dto);

            result.Id.Should().Be(post.Id);

            var updatedPost = await _context.Posts.FindAsync(post.Id);
            updatedPost.Title.Should().Be(dto.Title);
            updatedPost.Text.Should().Be(dto.Text);
        }

        [Fact]
        public async Task UpdatePostAsync_ShouldUpdateAssignment_WhenValid()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Old Task",
                Text = "Old text",
                Deadline = DateTime.UtcNow.AddDays(1),
                MaxScore = 5,
                TaskType = TaskType.Optional,
                SolvableAfterDeadline = false
            };
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var dto = new CreateUpdatePostDto
            {
                Type = PostType.TASK,
                Title = "New Task",
                Text = "New text",
                Deadline = DateTime.UtcNow.AddDays(10),
                MaxScore = 10,
                TaskType = TaskType.Mandatory,
                SolvableAfterDeadline = true
            };

            _validator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var result = await _service.UpdatePostAsync(teacherId, assignment.Id, dto);

            result.Id.Should().Be(assignment.Id);

            var updated = await _context.Assignments.FindAsync(assignment.Id);
            updated.Title.Should().Be(dto.Title);
            updated.Text.Should().Be(dto.Text);
            updated.Deadline.Should().Be(dto.Deadline);
            updated.MaxScore.Should().Be((uint)dto.MaxScore);
            updated.TaskType.Should().Be(dto.TaskType.Value);
            updated.SolvableAfterDeadline.Should().Be(dto.SolvableAfterDeadline.Value);
        }

        #endregion

        #region DeletePostAsync Tests

        [Fact]
        public async Task DeletePostAsync_ShouldThrow_WhenPostNotFound()
        {
            var userId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.DeletePostAsync(userId, Guid.NewGuid()));
        }

        [Fact]
        public async Task DeletePostAsync_ShouldThrow_WhenUserNotTeacher()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Post"
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.DeletePostAsync(studentId, post.Id));
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeleteRegularPost_WhenTeacher()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();

            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Post"
            };
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            var result = await _service.DeletePostAsync(teacherId, post.Id);

            result.Id.Should().Be(post.Id);
            var deleted = await _context.Posts.FindAsync(post.Id);
            deleted.Should().BeNull();
        }

        [Fact]
        public async Task DeletePostAsync_ShouldDeleteAssignment_WhenTeacher()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();

            var assignment = new Assignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                Title = "Task"
            };
            _context.Assignments.Add(assignment);
            await _context.SaveChangesAsync();

            var result = await _service.DeletePostAsync(teacherId, assignment.Id);

            result.Id.Should().Be(assignment.Id);
            var deleted = await _context.Assignments.FindAsync(assignment.Id);
            deleted.Should().BeNull();
        }

        #endregion

        #region GetCourseFeedAsync Tests

        [Fact]
        public async Task GetCourseFeedAsync_ShouldThrow_WhenUserNotMember()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var outsiderId = Guid.NewGuid();

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.GetCourseFeedAsync(outsiderId, courseId, 0, 10));
        }

        [Fact]
        public async Task GetCourseFeedAsync_ShouldReturnPagedFeed_WhenMember()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);
            for (int i = 0; i < 5; i++)
            {
                var post = new RegularPost
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    Title = $"Post {i}",
                    Text = "Text",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-i)
                };
                _context.Posts.Add(post);
            }

            for (int i = 0; i < 3; i++)
            {
                var assignment = new Assignment
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    Title = $"Task {i}",
                    Text = "Text",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-(i + 10))
                };
                _context.Assignments.Add(assignment);
            }
            await _context.SaveChangesAsync();

            var result = await _service.GetCourseFeedAsync(studentId, courseId, 0, 4);

            result.Records.Should().HaveCount(4);
            result.TotalRecords.Should().Be(8); 

            var dates = result.Records.Select(r => r.CreatedDate).ToList();
            dates.Should().BeInDescendingOrder();
        }

        [Fact]
        public async Task GetCourseFeedAsync_ShouldRespectSkipTake()
        {
            var (teacherId, courseId) = await SeedTeacherAndCourse();
            var studentId = await SeedStudent(courseId);

            for (int i = 0; i < 10; i++)
            {
                var post = new RegularPost
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    Title = $"Post {i}",
                    Text = "Text",
                    CreatedDate = DateTime.UtcNow.AddMinutes(-i)
                };
                _context.Posts.Add(post);
            }
            await _context.SaveChangesAsync();

            var result = await _service.GetCourseFeedAsync(studentId, courseId, 2, 3);

            result.Records.Should().HaveCount(3);
            result.TotalRecords.Should().Be(10);
            result.Records.Select(r => r.Title).Should().Equal("Post 2", "Post 3", "Post 4");
        }

        #endregion
    }
}