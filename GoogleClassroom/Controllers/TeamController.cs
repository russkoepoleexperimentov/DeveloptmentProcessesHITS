
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers
{
    [ApiController]
    [Route("api")]
    public class TeamController : ControllerBase
    {
        private readonly ITeamManagerService _teamManager;

        public TeamController(ITeamManagerService teamManager)
        {
            _teamManager = teamManager;
        }

        /// <summary>
        /// Студент: присоединиться к команде (если политика задания разрешает)
        /// </summary>
        [HttpPost("teams/{teamId}/join")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> JoinTeam(Guid teamId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            await _teamManager.JoinTeamAsync(teamId, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Joined team",
                Data = null
            });
        }

        /// <summary>
        /// Студент: выйти из команды (если политика задания разрешает)
        /// </summary>
        [HttpPost("teams/{teamId}/leave")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> LeaveTeam(Guid teamId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            await _teamManager.LeaveTeamAsync(teamId, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Left team",
                Data = null
            });
        }

        /// <summary>
        /// Студент: передать капитанство другому участнику команды (если политика задания разрешает)
        /// </summary>
        [HttpPost("teams/{teamId}/transfer-captain/{toUserId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> TransferCaptain(Guid teamId, Guid toUserId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            await _teamManager.TransferCaptainAsync(teamId, toUserId, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Captain transferred",
                Data = null
            });
        }

        /// <summary>
        /// Учитель: переименовать команду
        /// </summary>
        [HttpPut("teacher/teams/{teamId}/rename")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RenameTeam(Guid teamId, [FromBody] RenameTeamRequestDto dto)
        {
            var teacherId = HttpContext.GetUserId()!.Value;
            await _teamManager.RenameTeamAsync(teamId, dto.NewName, teacherId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Team renamed",
                Data = null
            });
        }

        /// <summary>
        /// Учитель: принудительно добавить студента в команду
        /// </summary>
        [HttpPost("teacher/teams/{teamId}/add-student/{studentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AddStudentToTeam(Guid teamId, Guid studentId)
        {
            var teacherId = HttpContext.GetUserId()!.Value;
            await _teamManager.AddStudentToTeamAsync(teamId, studentId, teacherId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Student added",
                Data = null
            });
        }

        /// <summary>
        /// Учитель: принудительно удалить студента из команды
        /// </summary>
        [HttpDelete("teacher/teams/{teamId}/remove-student/{studentId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoveStudentFromTeam(Guid teamId, Guid studentId)
        {
            var teacherId = HttpContext.GetUserId()!.Value;
            await _teamManager.RemoveStudentFromTeamAsync(teamId, studentId, teacherId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Student removed",
                Data = null
            });
        }

        /// <summary>
        /// Учитель: получить список всех команд задания
        /// </summary>
        [HttpGet("teacher/team-task/{assignmentId}/teams")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<List<TeamDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTeamsForAssignment(Guid assignmentId)
        {
            var teacherId = HttpContext.GetUserId()!.Value;
            var teams = await _teamManager.GetTeamsForAssignmentAsync(assignmentId, teacherId);
            return Ok(new ApiResponse<List<TeamDto>>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = teams
            });
        }

        /// <summary>
        /// Студент: получить свою команду для задания
        /// </summary>
        [HttpGet("team-task/{assignmentId}/my-team")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMyTeam(Guid assignmentId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var team = await _teamManager.GetMyTeamForAssignmentAsync(assignmentId, userId);
            if (team == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Type = ApiResponseType.Error,
                    Message = "You are not in a team for this assignment",
                    Data = null
                });
            }
            return Ok(new ApiResponse<TeamDto>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = team
            });
        }

        /// <summary>
        /// Проверить, является ли текущий пользователь капитаном команды
        /// </summary>
        [HttpGet("teams/{teamId}/is-captain")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        public async Task<IActionResult> IsCaptain(Guid teamId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            var isCaptain = await _teamManager.IsCaptainAsync(teamId, userId);
            return Ok(new ApiResponse<bool>
            {
                Type = ApiResponseType.Success,
                Message = null,
                Data = isCaptain
            });
        }

        /// <summary>
        /// Начать голосование за капитана (только для режима VotingAndLottery)
        /// </summary>
        [HttpPost("teams/{teamId}/start-voting")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> StartVoting(Guid teamId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            await _teamManager.StartVotingAsync(teamId, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Voting started",
                Data = null
            });
        }

        /// <summary>
        /// Проголосовать за кандидата (только для режима VotingAndLottery)
        /// </summary>
        [HttpPost("teams/{teamId}/vote/{candidateId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CastVote(Guid teamId, Guid candidateId)
        {
            var userId = HttpContext.GetUserId()!.Value;
            await _teamManager.CastVoteAsync(teamId, candidateId, userId);
            return Ok(new ApiResponse<object>
            {
                Type = ApiResponseType.Success,
                Message = "Vote cast",
                Data = null
            });
        }
    }
}