using Application.DTOs.Post;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Domain.Models;
using FluentValidation;
using GoogleClass.DTOs;
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
            if (course == null) throw new NotFoundException("Course not found");

            var userRole = await _context.CourseRoles.FirstOrDefaultAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can create posts");

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            GenericPost post = dto.Type switch
            {
                PostType.POST => CreateRegularPost(dto, courseId, currentUserId),
                PostType.TASK => CreateAssignment(dto, courseId, currentUserId),
                PostType.TEAM_TASK => await CreateTeamAssignmentAsync(dto, courseId, currentUserId),
                _ => throw new BadRequestException("Invalid post type")
            };

            await _context.SaveChangesAsync();

            if (dto.Files != null && dto.Files.Any())
                await AddFilesToPost(post.Id, dto.Files);

            return new IdRequestDto { Id = post.Id };
        }

        public async Task<PostDetailsDto> GetPostAsync(Guid currentUserId, Guid postId)
        {
            var post = await FindPostById(postId, includeFiles: true);
            if (post == null) throw new NotFoundException("Post not found");

            var isMember = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == post.CourseId && cr.UserId == currentUserId);
            if (!isMember) throw new ForbiddenException("You are not a member of this course");

            var response = MapToDetailsDto(post);

            if (post is Assignment assignment)
            {
                var solution = await _context.Solutions
                    .FirstOrDefaultAsync(s => s.TaskId == assignment.Id && s.UserId == currentUserId);

                if (solution != null)
                {
                    response.UserSolution = new UserSolutionDto
                    {
                        Id = solution.Id,
                        Text = solution.Text,
                        Score = solution.Score,
                        Status = solution.Status
                    };
                }
            }

            if (post is TeamAssignment teamAssignment)
            {
                var team = await _context.Teams
                    .Include(t => t.Members).ThenInclude(m => m.User)
                    .Where(t => t.CourseId == teamAssignment.CourseId &&
                                t.Members.Any(m => m.UserId == currentUserId))
                    .FirstOrDefaultAsync();

                if (team != null)
                {
                    var teamSolution = await _context.TeamSolutions
                        .FirstOrDefaultAsync(s => s.TaskId == teamAssignment.Id && s.TeamId == team.Id);
                    if (teamSolution != null)
                    {
                        response.TeamSolution = new TeamSolutionDto
                        {
                            Id = teamSolution.Id,
                            Text = teamSolution.Text,
                            Score = teamSolution.Score,
                            Status = teamSolution.Status,
                            Team = new TeamDto
                            {
                                Id = team.Id,
                                Name = team.Name,
                                Members = team.Members.Select(m => new TeamMemberDto
                                {
                                    UserId = m.UserId,
                                    Credentials = m.User.Credentials,
                                    Role = m.Role
                                }).ToList()
                            }
                        };
                    }
                }
            }

            return response;
        }

        public async Task<IdRequestDto> UpdatePostAsync(Guid currentUserId, Guid postId, CreateUpdatePostDto dto)
        {
            await _validator.ValidateAndThrowAsync(dto);
            var post = await FindPostById(postId, includeFiles: true);
            if (post == null) throw new NotFoundException("Post not found");

            var userRole = await _context.CourseRoles.FirstOrDefaultAsync(cr => cr.CourseId == post.CourseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can update posts");

            ValidateTypeMatch(post, dto.Type);

            if (dto.Files != null && dto.Files.Any())
                await ValidateFilesExist(dto.Files);

            post.Title = dto.Title;
            post.Text = dto.Text;
            post.UpdatedDate = DateTime.UtcNow;

            switch (post)
            {
                case Assignment assignment when dto.Type == PostType.TASK:
                    UpdateAssignment(assignment, dto);
                    break;
                case TeamAssignment teamAssignment when dto.Type == PostType.TEAM_TASK:
                    UpdateTeamAssignment(teamAssignment, dto);
                    await SyncTeamsForAssignmentAsync(teamAssignment, dto);
                    break;
            }

            await UpdatePostFiles(post, dto.Files);
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = post.Id };
        }

        public async Task<IdRequestDto> DeletePostAsync(Guid currentUserId, Guid postId)
        {
            var post = await FindPostById(postId, includeFiles: true);
            if (post == null) throw new NotFoundException("Post not found");

            var userRole = await _context.CourseRoles
                .FirstOrDefaultAsync(cr => cr.CourseId == post.CourseId && cr.UserId == currentUserId);
            if (userRole == null || userRole.RoleType != UserRoleType.Teacher)
                throw new ForbiddenException("Only teachers can delete posts");

            if (post.FilePosts != null && post.FilePosts.Any())
                _context.FilePosts.RemoveRange(post.FilePosts);

            _context.Remove(post);
            await _context.SaveChangesAsync();
            return new IdRequestDto { Id = post.Id };
        }

        public async Task<FeedResponseDto> GetCourseFeedAsync(Guid currentUserId, Guid courseId, int skip, int take)
        {
            var isMember = await _context.CourseRoles
                .AnyAsync(cr => cr.CourseId == courseId && cr.UserId == currentUserId);
            if (!isMember) throw new ForbiddenException("You are not a member of this course");

            var postsQuery = _context.Posts
                .Where(p => p.CourseId == courseId)
                .Select(p => new { p.Id, p.Title, p.CreatedDate, Type = "post" });

            var assignmentsQuery = _context.Assignments
                .Where(a => a.CourseId == courseId)
                .Select(a => new { a.Id, a.Title, a.CreatedDate, Type = "task" });

            var teamAssignmentsQuery = _context.TeamAssignments
                .Where(ta => ta.CourseId == courseId)
                .Select(ta => new { ta.Id, ta.Title, ta.CreatedDate, Type = "team_task" });

            var union = postsQuery.Union(assignmentsQuery).Union(teamAssignmentsQuery)
                .OrderByDescending(x => x.CreatedDate);

            var totalRecords = await union.CountAsync();
            var records = await union.Skip(skip).Take(take)
                .Select(x => new CourseFeedItemDto
                {
                    Id = x.Id,
                    Title = x.Title,
                    CreatedDate = x.CreatedDate,
                    Type = x.Type == "post" ? PostType.POST :
                           x.Type == "task" ? PostType.TASK : PostType.TEAM_TASK
                }).ToListAsync();

            return new FeedResponseDto { Records = records, TotalRecords = totalRecords };
        }

        #region Private Helpers

        private RegularPost CreateRegularPost(CreateUpdatePostDto dto, Guid courseId, Guid authorId)
        {
            var post = _mapper.Map<RegularPost>(dto);
            post.Id = Guid.NewGuid();
            post.CourseId = courseId;
            post.AuthorId = authorId;
            post.CreatedDate = DateTime.UtcNow;
            post.UpdatedDate = DateTime.UtcNow;

            _context.Posts.Add(post);
            return post;
        }

        private Assignment CreateAssignment(CreateUpdatePostDto dto, Guid courseId, Guid authorId)
        {
            var assignment = _mapper.Map<Assignment>(dto);
            assignment.Id = Guid.NewGuid();
            assignment.CourseId = courseId;
            assignment.AuthorId = authorId;
            assignment.CreatedDate = DateTime.UtcNow;
            assignment.UpdatedDate = DateTime.UtcNow;
            assignment.TaskType = dto.TaskType!.Value;
            assignment.MaxScore = (uint)(dto.MaxScore ?? 5);
            assignment.SolvableAfterDeadline = dto.SolvableAfterDeadline ?? false;

            _context.Assignments.Add(assignment);
            return assignment;
        }

        private void UpdateAssignment(Assignment assignment, CreateUpdatePostDto dto)
        {
            assignment.Deadline = dto.Deadline;
            assignment.MaxScore = (uint)(dto.MaxScore ?? 5);
            assignment.SolvableAfterDeadline = dto.SolvableAfterDeadline ?? false;
            assignment.TaskType = dto.TaskType!.Value;
        }

        private async Task<TeamAssignment> CreateTeamAssignmentAsync(CreateUpdatePostDto dto, Guid courseId, Guid authorId)
        {
            var teamAssignment = new TeamAssignment
            {
                Id = Guid.NewGuid(),
                CourseId = courseId,
                AuthorId = authorId,
                Title = dto.Title,
                Text = dto.Text ?? string.Empty,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Deadline = dto.Deadline,
                MaxScore = (uint)(dto.MaxScore ?? 5),
                SolvableAfterDeadline = dto.SolvableAfterDeadline ?? false,
                MinTeamSize = dto.MinTeamSize ?? 2,
                MaxTeamSize = dto.MaxTeamSize ?? 5,
                CaptainMode = dto.CaptainMode ?? CaptainSelectionMode.FirstMember,
                VotingDurationHours = dto.VotingDurationHours,
                PredefinedTeamsCount = dto.PredefinedTeamsCount ?? 0,
                AllowJoinTeam = dto.AllowJoinTeam ?? true,
                AllowLeaveTeam = dto.AllowLeaveTeam ?? true,
                AllowStudentTransferCaptain = dto.AllowStudentTransferCaptain ?? true,
                CopyGroupsFromPreviousAssignment = dto.CopyGroupsFromPreviousAssignment ?? false,
                SourceAssignmentId = dto.SourceAssignmentId
            };
            _context.TeamAssignments.Add(teamAssignment);
            await _context.SaveChangesAsync();
            await SyncTeamsForAssignmentAsync(teamAssignment, dto);
            return teamAssignment;
        }

        private void UpdateTeamAssignment(TeamAssignment teamAssignment, CreateUpdatePostDto dto)
        {
            teamAssignment.Deadline = dto.Deadline;
            teamAssignment.MaxScore = (uint)(dto.MaxScore ?? 5);
            teamAssignment.SolvableAfterDeadline = dto.SolvableAfterDeadline ?? false;
            teamAssignment.MinTeamSize = dto.MinTeamSize ?? teamAssignment.MinTeamSize;
            teamAssignment.MaxTeamSize = dto.MaxTeamSize ?? teamAssignment.MaxTeamSize;
            if (dto.CaptainMode.HasValue) teamAssignment.CaptainMode = dto.CaptainMode.Value;
            if (dto.VotingDurationHours.HasValue) teamAssignment.VotingDurationHours = dto.VotingDurationHours;
            if (dto.PredefinedTeamsCount.HasValue) teamAssignment.PredefinedTeamsCount = dto.PredefinedTeamsCount.Value;
            if (dto.AllowJoinTeam.HasValue) teamAssignment.AllowJoinTeam = dto.AllowJoinTeam.Value;
            if (dto.AllowLeaveTeam.HasValue) teamAssignment.AllowLeaveTeam = dto.AllowLeaveTeam.Value;
            if (dto.AllowStudentTransferCaptain.HasValue) teamAssignment.AllowStudentTransferCaptain = dto.AllowStudentTransferCaptain.Value;
            if (dto.CopyGroupsFromPreviousAssignment.HasValue) teamAssignment.CopyGroupsFromPreviousAssignment = dto.CopyGroupsFromPreviousAssignment.Value;
            if (dto.SourceAssignmentId.HasValue) teamAssignment.SourceAssignmentId = dto.SourceAssignmentId;
        }

        private async Task SyncTeamsForAssignmentAsync(TeamAssignment assignment, CreateUpdatePostDto dto)
        {
            bool copyFromPrevious = dto.CopyGroupsFromPreviousAssignment ?? assignment.CopyGroupsFromPreviousAssignment;
            Guid? sourceId = dto.SourceAssignmentId ?? assignment.SourceAssignmentId;

            if (copyFromPrevious && sourceId.HasValue)
            {
                await CopyTeamsFromPreviousAssignmentAsync(assignment, sourceId.Value);
                return;
            }

            int targetCount = dto.PredefinedTeamsCount ?? assignment.PredefinedTeamsCount;
            if (targetCount <= 0) return;

            var currentTeams = await _context.Teams
                .Where(t => t.AssignmentId == assignment.Id)
                .OrderBy(t => t.CreatedDate)
                .ToListAsync();

            if (currentTeams.Count < targetCount)
            {
                for (int i = currentTeams.Count + 1; i <= targetCount; i++)
                {
                    _context.Teams.Add(new Team
                    {
                        Id = Guid.NewGuid(),
                        CourseId = assignment.CourseId,
                        AssignmentId = assignment.Id,
                        Name = $"Команда {i}",
                        FixedCaptainId = null
                    });
                }
            }
            else if (currentTeams.Count > targetCount)
            {
                var toRemove = currentTeams.Skip(targetCount).ToList();
                foreach (var team in toRemove)
                {
                    bool hasMembers = await _context.TeamMembers.AnyAsync(m => m.TeamId == team.Id);
                    bool hasSolutions = await _context.TeamSolutions.AnyAsync(s => s.TeamId == team.Id);
                    if (hasMembers || hasSolutions)
                        throw new BadRequestException($"Нельзя удалить команду '{team.Name}' — есть участники или решения");
                    _context.Teams.Remove(team);
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task CopyTeamsFromPreviousAssignmentAsync(TeamAssignment newAssignment, Guid sourceAssignmentId)
        {
            var sourceTeams = await _context.Teams
                .Include(t => t.Members)
                .Where(t => t.AssignmentId == sourceAssignmentId)
                .ToListAsync();

            foreach (var srcTeam in sourceTeams)
            {
                var newTeam = new Team
                {
                    Id = Guid.NewGuid(),
                    CourseId = newAssignment.CourseId,
                    AssignmentId = newAssignment.Id,
                    Name = srcTeam.Name,
                    FixedCaptainId = srcTeam.FixedCaptainId 
                };
                _context.Teams.Add(newTeam);
                foreach (var member in srcTeam.Members)
                {
                    _context.TeamMembers.Add(new TeamMember
                    {
                        TeamId = newTeam.Id,
                        UserId = member.UserId,
                        JoinedAt = DateTime.UtcNow,
                        Role = member.Role
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task<GenericPost?> FindPostById(Guid postId, bool includeFiles = false)
        {
            GenericPost? post = null;
            if (includeFiles)
                post = await _context.Posts.Include(p => p.FilePosts).ThenInclude(fp => fp.File)
                    .FirstOrDefaultAsync(p => p.Id == postId);
            else
                post = await _context.Posts.FirstOrDefaultAsync(p => p.Id == postId);
            if (post != null) return post;

            if (includeFiles)
                post = await _context.Assignments.Include(a => a.FilePosts).ThenInclude(fp => fp.File)
                    .FirstOrDefaultAsync(a => a.Id == postId);
            else
                post = await _context.Assignments.FirstOrDefaultAsync(a => a.Id == postId);
            if (post != null) return post;

            if (includeFiles)
                post = await _context.TeamAssignments.Include(ta => ta.FilePosts).ThenInclude(fp => fp.File)
                    .FirstOrDefaultAsync(ta => ta.Id == postId);
            else
                post = await _context.TeamAssignments.FirstOrDefaultAsync(ta => ta.Id == postId);
            return post;
        }

        private void ValidateTypeMatch(GenericPost post, PostType requestedType)
        {
            var actualType = post switch
            {
                TeamAssignment => PostType.TEAM_TASK,
                Assignment => PostType.TASK,
                _ => PostType.POST
            };
            if (actualType != requestedType)
                throw new BadRequestException($"Post type mismatch. Expected {actualType}, got {requestedType}");
        }

        private PostDetailsDto MapToDetailsDto(GenericPost post)
        {
            var response = new PostDetailsDto
            {
                Id = post.Id,
                Title = post.Title,
                Text = post.Text,
                Files = post.FilePosts?.Select(fp => new FileDto
                {
                    Id = fp.FileId.ToString(),
                    Name = fp.File.OriginalName
                }).ToList()
            };

            switch (post)
            {
                case TeamAssignment teamAssignment:
                    response.Type = PostType.TEAM_TASK;
                    response.Deadline = teamAssignment.Deadline;
                    response.MaxScore = (int?)teamAssignment.MaxScore;
                    response.SolvableAfterDeadline = teamAssignment.SolvableAfterDeadline;
                    response.MinTeamSize = teamAssignment.MinTeamSize;
                    response.MaxTeamSize = teamAssignment.MaxTeamSize;

                    response.CaptainMode = teamAssignment.CaptainMode;
                    response.VotingDurationHours = teamAssignment.VotingDurationHours;
                    response.PredefinedTeamsCount = teamAssignment.PredefinedTeamsCount;
                    response.AllowJoinTeam = teamAssignment.AllowJoinTeam;
                    response.AllowLeaveTeam = teamAssignment.AllowLeaveTeam;
                    response.AllowStudentTransferCaptain = teamAssignment.AllowStudentTransferCaptain;

                    break;

                case Assignment assignment:
                    response.Type = PostType.TASK;
                    response.Deadline = assignment.Deadline;
                    response.MaxScore = (int?)assignment.MaxScore;
                    response.SolvableAfterDeadline = assignment.SolvableAfterDeadline;
                    response.TaskType = assignment.TaskType;
                    break;

                default:
                    response.Type = PostType.POST;
                    break;
            }

            return response;
        }

        private async Task AddFilesToPost(Guid postId, List<Guid> fileIds)
        {
            var filePosts = fileIds.Select(fileId => new FilePost
            {
                Id = Guid.NewGuid(),
                PostId = postId,
                FileId = fileId
            });
            _context.FilePosts.AddRange(filePosts);
            await _context.SaveChangesAsync();
        }

        private async Task UpdatePostFiles(GenericPost post, List<Guid>? fileIds)
        {
            if (post.FilePosts != null && post.FilePosts.Any())
                _context.FilePosts.RemoveRange(post.FilePosts);

            if (fileIds != null && fileIds.Any())
            {
                var newFilePosts = fileIds.Select(fileId => new FilePost
                {
                    Id = Guid.NewGuid(),
                    PostId = post.Id,
                    FileId = fileId
                });
                await _context.FilePosts.AddRangeAsync(newFilePosts);
            }
        }

        private async Task ValidateFilesExist(IEnumerable<Guid> fileIds)
        {
            var fileIdsList = fileIds.ToList();
            var existing = await _context.UserFiles.CountAsync(f => fileIdsList.Contains(f.Id));
            if (existing != fileIdsList.Count)
                throw new NotFoundException("One or more files not found");
        }

        #endregion
    }
}