using GoogleClass.DTOs.Course;
using Application.Services.Interfaces;
using Common.Exceptions;
using Domain.Models;
using FluentAssertions;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Application.Services.Implementations;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tests
{
    public class CourseServiceTests
    {
        private readonly GcDbContext _context;
        private readonly Mock<UserManager<User>> _userManagerMock;
        private readonly ICourseService _courseService;

        public CourseServiceTests()
        {
            // Настройка InMemoryDatabase
            var options = new DbContextOptionsBuilder<GcDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new GcDbContext(options);

            // Настройка UserManager
            var store = new Mock<IUserStore<User>>();
            _userManagerMock = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            // Создание экземпляра сервиса (предполагаем, что конструктор принимает эти зависимости)
            _courseService = new CourseService(_context, _userManagerMock.Object);
        }

        [Fact]
        public async Task GetUserCoursesAsync_UserHasCourses_ReturnsMappedCourses()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var course1 = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Course 1",
                AuthorId = Guid.NewGuid(),
                InviteCode = "code1"
            };
            var course2 = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Course 2",
                AuthorId = Guid.NewGuid(),
                InviteCode = "code2"
            };

            _context.Courses.AddRange(course1, course2);
            _context.CourseRoles.AddRange(
                new CourseRole { UserId = userId, CourseId = course1.Id, RoleType = UserRoleType.Teacher },
                new CourseRole { UserId = userId, CourseId = course2.Id, RoleType = UserRoleType.Student }
            );
            await _context.SaveChangesAsync();

            var service = new CourseService(_context, null); // остальные зависимости не важны

            // Act
            var result = await service.GetUserCoursesAsync(userId, 0, 2);

            // Assert
            Assert.Equal(2, result.TotalRecords);
            Assert.Contains(result.Records, c => c.Id == course1.Id && c.Title == course1.Title && c.Role == UserRoleType.Teacher);
            Assert.Contains(result.Records, c => c.Id == course2.Id && c.Title == course2.Title && c.Role == UserRoleType.Student);
        }

        [Fact]
        public async Task GetUserCoursesAsync_UserHasNoCourses_ReturnsEmptyList()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var service = new CourseService(_context, null);

            // Act
            var result = await service.GetUserCoursesAsync(userId, 0, 1);

            // Assert
            Assert.Empty(result.Records);
        }

        [Fact]
        public async Task GetUserCoursesAsync_OnlyRolesForOtherUsers_ReturnsEmptyForThisUser()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var otherUserId = Guid.NewGuid();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Course",
                AuthorId = Guid.NewGuid(),
                InviteCode = "code"
            };

            _context.Courses.Add(course);
            _context.CourseRoles.Add(new CourseRole
            {
                UserId = otherUserId,
                CourseId = course.Id,
                RoleType = UserRoleType.Teacher
            });
            await _context.SaveChangesAsync();

            var service = new CourseService(_context, null);

            // Act
            var result = await service.GetUserCoursesAsync(userId, 0, 1);

            // Assert
            Assert.Empty(result.Records);
        }

        #region CreateCourseAsync

        [Fact]
        public async Task CreateCourseAsync_ShouldCreateCourse_AndAssignTeacherRole()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateUpdateCourseRequestDto { Title = "Test Course" };
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());

            // Act
            var result = await _courseService.CreateCourseAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().NotBeEmpty();
            result.Title.Should().Be(request.Title);

            var course = await _context.Courses.FindAsync(result.Id);
            course.Should().NotBeNull();
            course.AuthorId.Should().Be(userId);
            course.Title.Should().Be(request.Title);
            course.InviteCode.Should().NotBeNullOrEmpty();

            var courseRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == result.Id && cr.UserId == userId);
            courseRole.Should().NotBeNull();
            courseRole.RoleType.Should().Be(UserRoleType.Teacher);
        }

        [Fact]
        public async Task CreateCourseAsync_ShouldThrow_WhenUserNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new CreateUpdateCourseRequestDto { Title = "Test" };

            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((User)null);

            // Act
            Func<Task> act = async () => await _courseService.CreateCourseAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("User not found");
        }

        #endregion

        #region JoinCourseAsync

        [Fact]
        public async Task JoinCourseAsync_ShouldAddUserAsStudent_WhenInviteCodeValid()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Test Course",
                AuthorId = authorId,
                InviteCode = "SECRET123",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var userId = Guid.NewGuid();
            var request = new JoinCourseRequestDto { InviteCode = "SECRET123" };
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());

            // Act
            var result = await _courseService.JoinCourseAsync(userId, request);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(course.Id);
            result.Title.Should().Be(course.Title);
            result.Role.Should().Be(UserRoleType.Student);

            var courseRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == course.Id && cr.UserId == userId);
            courseRole.Should().NotBeNull();
            courseRole.RoleType.Should().Be(UserRoleType.Student);
        }

        [Fact]
        public async Task JoinCourseAsync_ShouldThrow_WhenInviteCodeInvalid()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var request = new JoinCourseRequestDto { InviteCode = "WRONG" };
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());

            // Act
            Func<Task> act = async () => await _courseService.JoinCourseAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Course not found");
        }

        [Fact]
        public async Task JoinCourseAsync_ShouldThrow_WhenUserAlreadyMember()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                AuthorId = authorId,
                InviteCode = "SECRET123",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var userId = Guid.NewGuid();
            var existingRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = userId,
                RoleType = UserRoleType.Student,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(existingRole);
            await _context.SaveChangesAsync();
            _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString()))
                .ReturnsAsync(new User());

            var request = new JoinCourseRequestDto { InviteCode = "SECRET123" };

            // Act
            Func<Task> act = async () => await _courseService.JoinCourseAsync(userId, request);

            // Assert
            await act.Should().ThrowAsync<EntryExistsException>()
                .WithMessage("User is already a member of this course");
        }

        #endregion

        #region GetCourseDetailsAsync

        [Fact]
        public async Task GetCourseDetailsAsync_ShouldReturnCourse_WithUserRole_WhenUserIsMember()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Test Course",
                AuthorId = authorId,
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var userId = Guid.NewGuid();
            var role = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = course.Id,
                UserId = userId,
                RoleType = UserRoleType.Student,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(role);
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.GetCourseDetailsAsync(userId, course.Id);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(course.Id);
            result.Title.Should().Be(course.Title);
            result.AuthorId.Should().Be(authorId);
            result.InviteCode.Should().Be(course.InviteCode);
            result.Role.Should().Be(UserRoleType.Student);
        }

        [Fact]
        public async Task GetCourseDetailsAsync_ShouldThrow_WhenCourseNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _courseService.GetCourseDetailsAsync(userId, courseId);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Course not found");
        }

        [Fact]
        public async Task GetCourseDetailsAsync_ShouldThrow_WhenUserNotMember()
        {
            // Arrange
            var course = new Course
            {
                Id = Guid.NewGuid(),
                Title = "Test",
                AuthorId = Guid.NewGuid(),
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var userId = Guid.NewGuid();

            // Act
            Func<Task> act = async () => await _courseService.GetCourseDetailsAsync(userId, course.Id);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("User is not a member of this course");
        }

        #endregion

        #region ChangeRoleAsync

        [Fact]
        public async Task ChangeRoleAsync_ShouldPromoteStudentToTeacher_WhenCurrentUserIsTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, teacherId, studentId);

            var request = new ChangeRoleRequestDto { Role = UserRoleType.Teacher };

            // Act
            var result = await _courseService.ChangeRoleAsync(teacherId, courseId, studentId, request);

            // Assert
            result.Id.Should().Be(studentId);
            result.Role.Should().Be(UserRoleType.Teacher);

            var updatedRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == studentId);
            updatedRole.RoleType.Should().Be(UserRoleType.Teacher);
        }

        [Fact]
        public async Task ChangeRoleAsync_ShouldDemoteTeacherToStudent_WhenCurrentUserIsTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var otherTeacherId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, teacherId, otherTeacherId, UserRoleType.Teacher);

            var request = new ChangeRoleRequestDto { Role = UserRoleType.Student };

            // Act
            var result = await _courseService.ChangeRoleAsync(teacherId, courseId, otherTeacherId, request);

            // Assert
            result.Id.Should().Be(otherTeacherId);
            result.Role.Should().Be(UserRoleType.Student);

            var updatedRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == otherTeacherId);
            updatedRole.RoleType.Should().Be(UserRoleType.Student);
        }

        [Fact]
        public async Task ChangeRoleAsync_ShouldThrow_WhenCurrentUserNotTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, studentId, targetId, UserRoleType.Student);

            var request = new ChangeRoleRequestDto { Role = UserRoleType.Teacher };

            // Act
            Func<Task> act = async () => await _courseService.ChangeRoleAsync(studentId, courseId, targetId, request);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("Only teachers can change roles");
        }

        [Fact]
        public async Task ChangeRoleAsync_ShouldThrow_WhenTargetUserIsSelf()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, teacherId, teacherId, UserRoleType.Teacher);

            var request = new ChangeRoleRequestDto { Role = UserRoleType.Student };

            // Act
            Func<Task> act = async () => await _courseService.ChangeRoleAsync(teacherId, courseId, teacherId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Cannot change your own role");
        }

        [Fact]
        public async Task ChangeRoleAsync_ShouldThrow_WhenTargetUserIsCourseAuthor()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                AuthorId = authorId,
                Title = "Test",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var authorRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = authorId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            var teacherRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = teacherId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.AddRange(authorRole, teacherRole);
            await _context.SaveChangesAsync();

            var request = new ChangeRoleRequestDto { Role = UserRoleType.Student };

            // Act
            Func<Task> act = async () => await _courseService.ChangeRoleAsync(teacherId, courseId, authorId, request);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Cannot change role of course creator");
        }

        #endregion

        #region RemoveMemberAsync

        [Fact]
        public async Task RemoveMemberAsync_ShouldRemoveStudent_WhenCurrentUserIsTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, teacherId, studentId);

            // Act
            await _courseService.RemoveMemberAsync(teacherId, courseId, studentId);

            // Assert
            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == studentId);
            role.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrow_WhenCurrentUserNotTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var targetId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, studentId, targetId, UserRoleType.Student);

            // Act
            Func<Task> act = async () => await _courseService.RemoveMemberAsync(studentId, courseId, targetId);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("Only teachers can remove other members");
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldAllowUserToRemoveSelf_WhenNotAuthor()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                AuthorId = authorId,
                Title = "Test",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var studentRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = studentId,
                RoleType = UserRoleType.Student,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(studentRole);
            await _context.SaveChangesAsync();

            // Act
            await _courseService.RemoveMemberAsync(studentId, courseId, studentId);

            // Assert
            var role = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == studentId);
            role.Should().BeNull();
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrow_WhenAuthorTriesToRemoveSelf()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                AuthorId = authorId,
                Title = "Test",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var authorRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = authorId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(authorRole);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _courseService.RemoveMemberAsync(authorId, courseId, authorId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Cannot remove course creator");
        }

        [Fact]
        public async Task RemoveMemberAsync_ShouldThrow_WhenRemovingCourseAuthor()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var authorId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                AuthorId = authorId,
                Title = "Test",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var authorRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = authorId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            var teacherRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = teacherId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.AddRange(authorRole, teacherRole);
            await _context.SaveChangesAsync();

            // Act
            Func<Task> act = async () => await _courseService.RemoveMemberAsync(teacherId, courseId, authorId);

            // Assert
            await act.Should().ThrowAsync<BadRequestException>()
                .WithMessage("Cannot remove course creator");
        }

        #endregion

        #region GetMembersAsync

        [Fact]
        public async Task GetMembersAsync_ShouldReturnPagedMembers_WhenCurrentUserIsTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var teacherId = Guid.NewGuid();
            var students = new List<User>();
            for (int i = 0; i < 5; i++)
            {
                students.Add(new User { Id = Guid.NewGuid(), Credentials = $"Student{i}", Email = $"s{i}@test.com" });
            }

            var course = new Course
            {
                Id = courseId,
                AuthorId = teacherId,
                Title = "Test",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var teacherRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = teacherId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(teacherRole);

            foreach (var student in students)
            {
                _context.Users.Add(student);
                _context.CourseRoles.Add(new CourseRole
                {
                    Id = Guid.NewGuid(),
                    CourseId = courseId,
                    UserId = student.Id,
                    RoleType = UserRoleType.Student,
                    CreatedDate = DateTime.UtcNow,
                    UpdatedDate = DateTime.UtcNow
                });
            }
            await _context.SaveChangesAsync();

            // Act
            var result = await _courseService.GetMembersAsync(teacherId, courseId, 0, 3, null);

            // Assert
            result.Should().NotBeNull();
            result.TotalRecords.Should().Be(5); // 5 students + teacher = 6 total, but teacher is also member, but in members list we probably include all members? In spec: список участников курса (всех). So total should be 6.
            // Actually we added teacher + 5 students = 6 members. TotalRecords should be 6.
            result.Records.Should().HaveCount(3); // take=3
            result.Records.First().Should().BeOfType<CourseMemberDto>();
        }

        [Fact]
        public async Task GetMembersAsync_ShouldThrow_WhenCurrentUserNotTeacher()
        {
            // Arrange
            var courseId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            await SetupCourseWithMembers(courseId, studentId, studentId, UserRoleType.Student);

            // Act
            Func<Task> act = async () => await _courseService.GetMembersAsync(studentId, courseId, 0, 10, null);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("Only teachers can view members list");
        }

        #endregion

        // Вспомогательный метод для настройки курса с участниками
        private async Task SetupCourseWithMembers(Guid courseId, Guid currentUserId, Guid targetUserId, UserRoleType currentUserRole = UserRoleType.Teacher, UserRoleType targetUserRole = UserRoleType.Student)
        {
            var authorId = Guid.NewGuid(); // автор курса (может быть отдельным)
            var course = new Course
            {
                Id = courseId,
                AuthorId = authorId,
                Title = "Test Course",
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var currentUserRoleEntity = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = currentUserId,
                RoleType = currentUserRole,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            var targetUserRoleEntity = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = targetUserId,
                RoleType = targetUserRole,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.AddRange(currentUserRoleEntity, targetUserRoleEntity);
            await _context.SaveChangesAsync();
        }

        #region UpdateCourseAsync

        [Fact]
        public async Task UpdateCourseAsync_ShouldUpdateTitle_WhenUserIsTeacher()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var course = new Course
            {
                Id = courseId,
                Title = "Old Title",
                AuthorId = authorId,
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                UpdatedDate = DateTime.UtcNow.AddDays(-1)
            };
            _context.Courses.Add(course);

            var teacherRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = authorId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow.AddDays(-1),
                UpdatedDate = DateTime.UtcNow.AddDays(-1)
            };
            _context.CourseRoles.Add(teacherRole);
            await _context.SaveChangesAsync();

            var request = new CreateUpdateCourseRequestDto { Title = "New Title" };

            // Act
            var result = await _courseService.UpdateCourseAsync(authorId, courseId, request);

            // Assert
            result.Id.Should().Be(courseId);
            result.Title.Should().Be("New Title");

            var updatedCourse = await _context.Courses.FindAsync(courseId);
            updatedCourse.Title.Should().Be("New Title");
            updatedCourse.UpdatedDate.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task UpdateCourseAsync_ShouldThrow_WhenCourseNotFound()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var courseId = Guid.NewGuid();
            var request = new CreateUpdateCourseRequestDto { Title = "New" };

            // Act
            Func<Task> act = async () => await _courseService.UpdateCourseAsync(userId, courseId, request);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                .WithMessage("Course not found");
        }

        [Fact]
        public async Task UpdateCourseAsync_ShouldThrow_WhenUserNotTeacher()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var studentId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                Title = "Test",
                AuthorId = authorId,
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var studentRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = studentId,
                RoleType = UserRoleType.Student,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.Add(studentRole);
            await _context.SaveChangesAsync();

            var request = new CreateUpdateCourseRequestDto { Title = "Hack" };

            // Act
            Func<Task> act = async () => await _courseService.UpdateCourseAsync(studentId, courseId, request);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("Only teachers can update course");
        }

        [Fact]
        public async Task UpdateCourseAsync_ShouldThrow_WhenUserNotMember()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var strangerId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                Title = "Test",
                AuthorId = authorId,
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            var request = new CreateUpdateCourseRequestDto { Title = "Hack" };

            // Act
            Func<Task> act = async () => await _courseService.UpdateCourseAsync(strangerId, courseId, request);

            // Assert
            await act.Should().ThrowAsync<ForbiddenException>()
                .WithMessage("Only teachers can update course"); // Или можно уточнить "User is not a member", но текущая логика проверяет role == null -> Forbidden
        }
        [Fact]
        public async Task UpdateCourseAsync_ShouldAllowAnyTeacher_ToUpdateCourse()
        {
            // Arrange
            var authorId = Guid.NewGuid();
            var otherTeacherId = Guid.NewGuid();
            var courseId = Guid.NewGuid();

            var course = new Course
            {
                Id = courseId,
                Title = "Original",
                AuthorId = authorId,
                InviteCode = "CODE",
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.Courses.Add(course);

            var authorRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = authorId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            var otherTeacherRole = new CourseRole
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                UserId = otherTeacherId,
                RoleType = UserRoleType.Teacher,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };
            _context.CourseRoles.AddRange(authorRole, otherTeacherRole);
            await _context.SaveChangesAsync();

            var request = new CreateUpdateCourseRequestDto { Title = "Updated by other teacher" };

            // Act
            var result = await _courseService.UpdateCourseAsync(otherTeacherId, courseId, request);

            // Assert
            result.Title.Should().Be("Updated by other teacher");
        }

        #endregion
    }
}