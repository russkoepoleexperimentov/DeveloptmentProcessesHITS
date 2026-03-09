using Application.DTOs.Post;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Post;

namespace Application.Services.Interfaces
{
    public interface IPostService
    {
        Task<IdRequestDto> CreatePostAsync(Guid currentUserId, Guid courseId, CreateUpdatePostDto dto);
        Task<PostDetailsDto> GetPostAsync(Guid currentUserId, Guid postId);
        Task<IdRequestDto> UpdatePostAsync(Guid currentUserId, Guid postId, CreateUpdatePostDto dto);
        Task<IdRequestDto> DeletePostAsync(Guid currentUserId, Guid postId);
        Task<FeedResponseDto> GetCourseFeedAsync(Guid currentUserId, Guid courseId, int skip, int take);
    }
}
