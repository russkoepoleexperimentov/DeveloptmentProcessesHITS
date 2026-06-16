using Application.DTOs.Grading.PeerReview;
using GoogleClass.DTOs.Common;

namespace Application.Services.Interfaces;

public interface IPeerReviewService
{
    Task<PeerReviewTargetDto?> GetNextAssignmentAsync(Guid currentUserId, Guid taskId);

    Task<PeerReviewProgressDto> SubmitReviewAsync(Guid currentUserId, Guid reviewId, SubmitPeerReviewDto dto);

    Task<PeerReviewProgressDto> GetIndividualProgressAsync(Guid currentUserId, Guid taskId);

    Task<PeerReviewProgressDto> FinishAsync(Guid currentUserId, Guid taskId);

    Task<PagedResponse<PeerReviewTeamTargetDto>> GetAvailableTeamSolutionsAsync(Guid currentUserId, Guid taskId);

    Task<PeerReviewProgressDto> SubmitTeamReviewAsync(Guid currentUserId, Guid teamSolutionId, SubmitPeerReviewDto dto);

    Task<PeerReviewProgressDto> GetTeamProgressAsync(Guid currentUserId, Guid taskId);

    Task<PeerReviewProgressDto?> GetIndividualProgressOrNullAsync(Guid currentUserId, Guid taskId);

    Task<PeerReviewProgressDto?> GetTeamProgressOrNullAsync(Guid currentUserId, Guid taskId);
}
