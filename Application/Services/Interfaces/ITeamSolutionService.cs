using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;

namespace Application.Services.Interfaces;

public interface ITeamSolutionService
{
    Task<IdRequestDto> SubmitSolutionAsync(Guid currentUserId, Guid taskId, SubmitTeamSolutionRequestDto dto);
    Task<IdRequestDto> DeleteSolutionAsync(Guid currentUserId, Guid taskId);
    Task<StudentTeamSolutionDetailsDto> GetMySolutionAsync(Guid currentUserId, Guid taskId);
    Task<TeamSolutionListDto> GetSolutionListAsync(
        Guid currentUserId,
        Guid taskId,
        int skip,
        int take,
        SolutionStatus? status,
        Guid? teamId);
    Task<IdRequestDto> MarkSolutionAsync(Guid currentUserId, Guid solutionId, UpdateTeamSolutionRequestDto dto);
}