using Application.Services.Implementations;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Domain.Models;
using FluentAssertions;
using FluentValidation;
using GoogleClass.Common;
using GoogleClass.DTOs.Comment;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Tests
{
    namespace Tests
{
    public class CommentServiceTests
    {
        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<IMapper> _mapper;
        private readonly Mock<IValidator<AddCommentRequestDto>> _addValidator;
        private readonly Mock<IValidator<EditCommentRequestDto>> _editValidator;

        private readonly GcDbContext _context;
        private readonly ICommentService _service;

        public CommentServiceTests()
        {
            var store = new Mock<IUserStore<User>>();

            _userManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            _mapper = new Mock<IMapper>();
            _addValidator = new Mock<IValidator<AddCommentRequestDto>>();
            _editValidator = new Mock<IValidator<EditCommentRequestDto>>();

            var options = new DbContextOptionsBuilder<GcDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new GcDbContext(options);
            
            _service = new CommentService(
                _context,
                _userManager.Object,
                _addValidator.Object,
                _editValidator.Object);
        }

        private async Task<Guid> SeedPost()
        {
            var post = new RegularPost
            {
                Id = Guid.NewGuid(),
                Title = "Test Post",
                Text = "Content"
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return post.Id;
        }

        private async Task<Guid> SeedSolution(Guid userId)
        {
            var solution = new Solution
            {
                Text = "",
                Id = Guid.NewGuid(),
                UserId = userId
            };

            _context.Solutions.Add(solution);
            await _context.SaveChangesAsync();

            return solution.Id;
        }

        private async Task<Comment> SeedComment(Guid userId, Guid commentableId)
        {
            var comment = new Comment
            {
                Id = Guid.NewGuid(),
                Text = "Test comment",
                UserId = userId,
                CommentableId = commentableId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return comment;
        }

        #region CreatePostCommentAsync

        [Fact]
        public async Task CreatePostCommentAsync_ShouldThrow_WhenPostNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new AddCommentRequestDto { Text = "Hello" };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreatePostCommentAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreatePostCommentAsync_ShouldCreateComment_WhenValid()
        {
            var userId = Guid.NewGuid();
            var postId = await SeedPost();

            var dto = new AddCommentRequestDto { Text = "Comment" };

            _addValidator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var mapped = new Comment { Text = dto.Text };

            _mapper.Setup(m => m.Map<Comment>(dto)).Returns(mapped);

            var result = await _service.CreatePostCommentAsync(userId, postId, dto);

            result.Id.Should().NotBeEmpty();

            var saved = await _context.Comments.FindAsync(result.Id);

            saved.Should().NotBeNull();
            saved.Text.Should().Be(dto.Text);
            saved.UserId.Should().Be(userId);
            saved.CommentableId.Should().Be(postId);
        }

        #endregion

        #region CreateSolutionCommentAsync

        [Fact]
        public async Task CreateSolutionCommentAsync_ShouldThrow_WhenSolutionNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new AddCommentRequestDto { Text = "Comment" };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateSolutionCommentAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateSolutionCommentAsync_ShouldThrow_WhenUserNotAuthor()
        {
            var userId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();

            var solutionId = await SeedSolution(otherUser);

            var dto = new AddCommentRequestDto { Text = "Comment" };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.CreateSolutionCommentAsync(userId, solutionId, dto));
        }

        #endregion

        #region CreateCommentReplyAsync

        [Fact]
        public async Task CreateCommentReplyAsync_ShouldThrow_WhenCommentNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new AddCommentRequestDto { Text = "Reply" };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.CreateCommentReplyAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task CreateCommentReplyAsync_ShouldCreateReply_WhenValid()
        {
            var userId = Guid.NewGuid();
            var postId = await SeedPost();

            var root = await SeedComment(userId, postId);

            var dto = new AddCommentRequestDto { Text = "Reply" };

            _addValidator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var mapped = new Comment { Text = dto.Text };

            _mapper.Setup(m => m.Map<Comment>(dto)).Returns(mapped);

            var result = await _service.CreateCommentReplyAsync(userId, root.Id, dto);

            var reply = await _context.Comments.FindAsync(result.Id);

            reply.Should().NotBeNull();
            reply.ParentCommentId.Should().Be(root.Id);
            reply.CommentableId.Should().Be(root.CommentableId);
        }

        #endregion

        #region EditCommentAsync

        [Fact]
        public async Task EditCommentAsync_ShouldThrow_WhenCommentNotFound()
        {
            var userId = Guid.NewGuid();
            var dto = new EditCommentRequestDto { Text = "Updated" };

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.EditCommentAsync(userId, Guid.NewGuid(), dto));
        }

        [Fact]
        public async Task EditCommentAsync_ShouldThrow_WhenUserNotAuthor()
        {
            var userId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();

            var postId = await SeedPost();
            var comment = await SeedComment(otherUser, postId);

            var dto = new EditCommentRequestDto { Text = "Updated" };

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.EditCommentAsync(userId, comment.Id, dto));
        }

        [Fact]
        public async Task EditCommentAsync_ShouldUpdateComment_WhenValid()
        {
            var userId = Guid.NewGuid();
            var postId = await SeedPost();

            var comment = await SeedComment(userId, postId);

            var dto = new EditCommentRequestDto { Text = "Updated text" };

            _editValidator.Setup(v => v.ValidateAsync(dto, default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            var result = await _service.EditCommentAsync(userId, comment.Id, dto);

            var updated = await _context.Comments.FindAsync(comment.Id);

            result.Id.Should().Be(comment.Id);
            updated.Text.Should().Be(dto.Text);
        }

        #endregion

        #region DeleteCommentAsync

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrow_WhenCommentNotFound()
        {
            var userId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.DeleteCommentAsync(userId, Guid.NewGuid()));
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldThrow_WhenUserNotAuthor()
        {
            var userId = Guid.NewGuid();
            var otherUser = Guid.NewGuid();

            var postId = await SeedPost();
            var comment = await SeedComment(otherUser, postId);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.DeleteCommentAsync(userId, comment.Id));
        }

        [Fact]
        public async Task DeleteCommentAsync_ShouldDeleteComment_WhenAuthor()
        {
            var userId = Guid.NewGuid();
            var postId = await SeedPost();

            var comment = await SeedComment(userId, postId);

            var result = await _service.DeleteCommentAsync(userId, comment.Id);

            result.Id.Should().Be(comment.Id);

            var deleted = await _context.Comments.FindAsync(comment.Id);

            deleted.Text.Should().Be(Constants.DELETED_COMMENT_TEXT);
        }

        #endregion

        #region GetCommentRepliesAsync

        [Fact]
        public async Task GetCommentRepliesAsync_ShouldThrow_WhenCommentNotFound()
        {
            var userId = Guid.NewGuid();

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetCommentRepliesAsync(userId, Guid.NewGuid()));
        }

        [Fact]
        public async Task GetCommentRepliesAsync_ShouldReturnReplies()
        {
            var userId = Guid.NewGuid();
            
            var user = new User
            {
                Id = userId,
                Credentials = "testuser"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            var postId = await SeedPost();

            var root = await SeedComment(userId, postId);

            var reply = new Comment
            {
                Id = Guid.NewGuid(),
                Text = "Reply",
                UserId = userId,
                ParentCommentId = root.Id,
                CommentableId = postId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow
            };

            _context.Comments.Add(reply);
            await _context.SaveChangesAsync();

            var result = await _service.GetCommentRepliesAsync(userId, root.Id);

            result.Should().HaveCount(1);
            result.First().Id.Should().Be(reply.Id);
        }

        #endregion
    }
}
}