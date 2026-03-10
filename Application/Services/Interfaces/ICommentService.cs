using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.Common;

namespace Application.Services.Interfaces;

public interface ICommentService
{
    
    Task<IdRequestDto> CreatePostCommentAsync(Guid currentUserId, Guid postId, AddCommentRequestDto dto);
    Task<IdRequestDto> CreateSolutionCommentAsync(Guid currentUserId, Guid solutionId, AddCommentRequestDto dto);
    Task<IdRequestDto> CreateCommentReplyAsync(Guid currentUserId, Guid commentId, AddCommentRequestDto dto);
    Task<IdRequestDto> EditCommentAsync(Guid currentUserId, Guid commentId, EditCommentRequestDto dto);
    Task<IdRequestDto> DeleteCommentAsync(Guid currentUserId, Guid commentId);
    Task<List<CommentDto>>  GetPostRootCommentAsync(Guid currentUserId, Guid postId);
    Task<List<CommentDto>>  GetSolutionRootCommentAsync(Guid currentUserId, Guid solutionId);
    Task<List<CommentDto>>  GetCommentRepliesAsync(Guid currentUserId, Guid commentId);
}