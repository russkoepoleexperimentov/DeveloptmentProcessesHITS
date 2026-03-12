using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Course;
using GoogleClass.DTOs.Course.Application.DTOs.User;

namespace Application.Services.Interfaces
{
    public interface ICourseService
    {
        Task<PagedResponse<UserCourseDto>> GetUserCoursesAsync(Guid userId, int skip, int take);
        Task<CreateUpdateCourseResponseDto> CreateCourseAsync(Guid userId, CreateUpdateCourseRequestDto request);
        Task<CreateUpdateCourseResponseDto> UpdateCourseAsync(Guid currentUserId, Guid courseId, CreateUpdateCourseRequestDto request);
        Task<JoinCourseResponseDto> JoinCourseAsync(Guid userId, JoinCourseRequestDto request);
        Task<CourseDetailsDto> GetCourseDetailsAsync(Guid userId, Guid courseId);
        Task<ChangeRoleResponseDto> ChangeRoleAsync(Guid currentUserId, Guid courseId, Guid targetUserId, ChangeRoleRequestDto request);
        Task RemoveMemberAsync(Guid currentUserId, Guid courseId, Guid targetUserId);
        Task<PagedResponse<CourseMemberDto>> GetMembersAsync(Guid currentUserId, Guid courseId, int skip, int take, string? query);
    }
}
