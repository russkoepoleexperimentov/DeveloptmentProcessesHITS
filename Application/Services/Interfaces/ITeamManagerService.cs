using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;

namespace Application.Services.Interfaces
{
    public interface ITeamManagerService
    {
        Task JoinTeamAsync(Guid teamId, Guid studentId);
        Task LeaveTeamAsync(Guid teamId, Guid studentId);
        Task TransferCaptainAsync(Guid teamId, Guid toStudentId, Guid currentStudentId);
        Task AddStudentToTeamAsync(Guid teamId, Guid studentId, Guid teacherId);
        Task RemoveStudentFromTeamAsync(Guid teamId, Guid studentId, Guid teacherId);
        Task RenameTeamAsync(Guid teamId, string newName, Guid teacherId);
        Task<List<TeamDto>> GetTeamsForAssignmentAsync(Guid assignmentId, Guid teacherId);
        Task<TeamDto?> GetMyTeamForAssignmentAsync(Guid assignmentId, Guid studentId);
        Task<bool> IsCaptainAsync(Guid teamId, Guid userId);

        Task StartVotingAsync(Guid teamId, Guid initiatorId);
        Task CastVoteAsync(Guid teamId, Guid candidateId, Guid voterId);
    }
}