// Application/Services/Interfaces/IGradeDistributionService.cs
using GoogleClass.DTOs.GradeDistribution;

namespace Application.Services.Interfaces;

public interface IGradeDistributionService
{
    Task<GradeDistributionResponseDto> GetDistributionAsync(Guid teamId, Guid assignmentId, Guid currentUserId);
    Task<GradeDistributionResponseDto> UpdateDistributionAsync(Guid teamId, Guid assignmentId, Guid currentUserId, GradeDistributionUpdateRequestDto dto);
    Task CastVoteAsync(Guid teamId, Guid assignmentId, Guid currentUserId, GradeDistributionVoteRequestDto dto);
    Task ResetDistributionAsync(Guid teamId, Guid assignmentId, uint newRawScore); // вызывается при изменении raw-оценки
}