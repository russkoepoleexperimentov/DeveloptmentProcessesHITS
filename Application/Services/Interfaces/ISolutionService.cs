using GoogleClass.DTOs;
using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;

namespace Application.Services.Interfaces;

public interface ISolutionService
{
    Task<IdRequestDto> SubmitSolutionAsync(Guid currentUserId, Guid taskId, SubmitSolutionRequestDto dto);
    Task<IdRequestDto> DeleteSolutionAsync(Guid currentUserId, Guid taskId);
    Task<StudentSolutionDetailsDto> GetSolutionByIdAsync(Guid currentUserId, Guid taskId);
    Task<SolutionListDto> GetSolutionListAsync(
        Guid currentUserId,
        Guid taskId,
        int skip,
        int take,
        SolutionStatus? status,
        Guid? studentId);
    Task<IdRequestDto> MarkSolutionAsync(Guid currentUserId, Guid solutionId, UpdateSolutionRequestDto dto);
}