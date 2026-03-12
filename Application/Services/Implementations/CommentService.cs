using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using FluentValidation;
using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using GoogleClass.Common;

namespace Application.Services.Implementations;

public class CommentService : ICommentService
{
    private readonly GcDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IValidator<AddCommentRequestDto> _addCommentValidator;
    private readonly IValidator<EditCommentRequestDto> _editCommentValidator;

    public CommentService(
        GcDbContext context,
        UserManager<User> userManager,
        IValidator<AddCommentRequestDto> addCommentValidator,
        IValidator<EditCommentRequestDto> editCommentValidator)
    {
        _context = context;
        _userManager = userManager;
        _addCommentValidator = addCommentValidator;
        _editCommentValidator = editCommentValidator;
    }

    public async Task<IdRequestDto> CreatePostCommentAsync(Guid currentUserId, Guid postId, AddCommentRequestDto dto)
    {
        await _addCommentValidator.ValidateAndThrowAsync(dto);
        
        var post = await _context.Posts.FindAsync(postId);
        var assignment = await _context.Assignments.FindAsync(postId);
        if(post == null && assignment == null)
            throw new NotFoundException("Post not found");

        var comment = CreateBaseComment(currentUserId, dto);
        comment.CommentableId = postId;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return new IdRequestDto { Id = comment.Id };
    }

    public async Task<IdRequestDto> CreateSolutionCommentAsync(Guid currentUserId, Guid solutionId, AddCommentRequestDto dto)
    {
        await _addCommentValidator.ValidateAndThrowAsync(dto);
        
        var solution = await _context.Solutions.FindAsync(solutionId);
        if(solution == null)
            throw new NotFoundException("Solution not found");
        
        var isTeacher = await _context.CourseRoles
            .AnyAsync(cr => cr.CourseId == solution.Task!.CourseId &&
                            cr.UserId == currentUserId &&
                            cr.RoleType == UserRoleType.Teacher);
        
        if(solution.UserId != currentUserId && !isTeacher)
            throw new ForbiddenException("Only teacher and an author of this solution can comment");

        var comment = CreateBaseComment(currentUserId, dto);
        comment.CommentableId = solutionId;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return new IdRequestDto { Id = comment.Id };
    }

    public async Task<IdRequestDto> CreateCommentReplyAsync(Guid currentUserId, Guid commentId, AddCommentRequestDto dto)
    {
        await _addCommentValidator.ValidateAndThrowAsync(dto);
        
        var rootComment = await _context.Comments.FindAsync(commentId);
        if(rootComment == null)
            throw new NotFoundException("Comment not found");

        var comment = CreateBaseComment(currentUserId, dto);
        comment.ParentCommentId = commentId;
        comment.CommentableId = rootComment.CommentableId;

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return new IdRequestDto { Id = comment.Id };
    }

    public async Task<IdRequestDto> EditCommentAsync(Guid currentUserId, Guid commentId, EditCommentRequestDto dto)
    {
        await _editCommentValidator.ValidateAndThrowAsync(dto);
        
        var comment = await _context.Comments.FindAsync(commentId);
        if(comment == null)
            throw new NotFoundException("Comment not found");
        
        if(comment.UserId != currentUserId)
            throw new ForbiddenException("User is not an author of this comment");
        
        comment.UpdatedDate = DateTime.UtcNow;
        comment.Text = dto.Text;
        
        await _context.SaveChangesAsync();

        return new IdRequestDto { Id = comment.Id };
    }

    public async Task<IdRequestDto> DeleteCommentAsync(Guid currentUserId, Guid commentId)
    {
        var comment = await _context.Comments.FindAsync(commentId);
        if(comment == null)
            throw new NotFoundException("Comment not found");
        
        if(comment.UserId != currentUserId)
            throw new ForbiddenException("User is not an author of this comment");
        
        comment.Text = Constants.DELETED_COMMENT_TEXT;
        comment.IsDeleted = true;
        comment.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new IdRequestDto { Id = comment.Id };
    }

    public async Task<List<CommentDto>> GetPostRootCommentAsync(Guid currentUserId, Guid postId)
    {
        var post = await _context.Posts.FindAsync(postId);
        var assignment = await _context.Assignments.FindAsync(postId);
        if (post == null && assignment == null)
            throw new NotFoundException("Post not found");

        var comments = await _context.Comments
            .Where(c => c.CommentableId == postId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text!,
                IsDeleted = c.IsDeleted,
                Author = new CommentAuthorDto
                {
                    Id = c.User.Id,
                    Credentials = c.User.Credentials!
                },
                NestedCount = c.Replies.Count
            })
            .ToListAsync();

        return comments;
    }

    public async Task<List<CommentDto>> GetSolutionRootCommentAsync(Guid currentUserId, Guid solutionId)
    {
        var solution = await _context.Solutions.FindAsync(solutionId);
        if (solution  == null)
            throw new NotFoundException("Solution not found");

        var comments = await _context.Comments
            .Where(c => c.CommentableId == solutionId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedDate)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text!,
                IsDeleted = c.IsDeleted,
                Author = new CommentAuthorDto
                {
                    Id = c.User.Id,
                    Credentials = c.User.Credentials!
                },
                NestedCount = c.Replies.Count
            })
            .ToListAsync();

        return comments;
    }

    public async Task<List<CommentDto>> GetCommentRepliesAsync(Guid currentUserId, Guid commentId)
    {
        var parentComment = await _context.Comments
            .AnyAsync(c => c.Id == commentId);

        if (!parentComment)
            throw new NotFoundException("Comment not found");

        var replies = await _context.Comments
            .Where(c => c.ParentCommentId == commentId)
            .OrderBy(c => c.CreatedDate)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Text = c.Text!,
                IsDeleted = c.IsDeleted,
                Author = new CommentAuthorDto
                {
                    Id = c.User.Id,
                    Credentials = c.User.Credentials!
                },
                NestedCount = c.Replies.Count
            })
            .ToListAsync();

        return replies;
    }
    
    private Comment CreateBaseComment(Guid currentUserId, AddCommentRequestDto dto)
    {
        var comment = new Comment();

        comment.Id = Guid.NewGuid();
        comment.Text = dto.Text;
        comment.CreatedDate = DateTime.UtcNow;
        comment.UpdatedDate = DateTime.UtcNow;
        comment.UserId = currentUserId;

        return comment;
    }
}