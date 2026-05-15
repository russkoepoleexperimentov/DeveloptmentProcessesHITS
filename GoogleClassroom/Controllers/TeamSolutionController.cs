using Application.DTOs.Grading;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Common;
using GoogleClass.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers;

[ApiController]
[Route("api")]
public class TeamSolutionController : ControllerBase
{
    private readonly ITeamSolutionService _teamSolutionService;

    public TeamSolutionController(ITeamSolutionService teamSolutionService)
    {
        _teamSolutionService = teamSolutionService;
    }

    /// <summary>
    /// Отправить командное решение (любой участник команды)
    /// </summary>
    [HttpPut("team-task/{taskId}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitSolution(Guid taskId, SubmitTeamSolutionRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.SubmitSolutionAsync(userId, taskId, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Удалить командное решение
    /// </summary>
    [HttpDelete("team-task/{taskId}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSolution(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.DeleteSolutionAsync(userId, taskId);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Получить решение своей команды
    /// </summary>
    [HttpGet("team-task/{taskId}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<StudentTeamSolutionDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySolution(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.GetMySolutionAsync(userId, taskId);
        return Ok(new ApiResponse<StudentTeamSolutionDetailsDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Получить список всех командных решений (для преподавателя)
    /// </summary>
    [HttpGet("team-task/{taskId}/solutions")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<TeamSolutionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolutionList(
        Guid taskId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] SolutionStatus? status = null,
        [FromQuery] Guid? teamId = null)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.GetSolutionListAsync(
            userId,
            taskId,
            skip,
            take,
            status,
            teamId);
        return Ok(new ApiResponse<TeamSolutionListDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Оценить командное решение (для преподавателя)
    /// </summary>
    [HttpPost("team-solution/{solutionId}/review")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewSolution(Guid solutionId, UpdateTeamSolutionRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.MarkSolutionAsync(userId, solutionId, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Подать или обновить свою самооценку для командного решения (любой участник команды)
    /// </summary>
    [HttpPut("team-task/{taskId}/self-assessment")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitSelfAssessment(Guid taskId, SubmitSelfAssessmentDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.SubmitSelfAssessmentAsync(userId, taskId, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Удалить свою самооценку для командного решения
    /// </summary>
    [HttpDelete("team-task/{taskId}/self-assessment")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSelfAssessment(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.DeleteSelfAssessmentAsync(userId, taskId);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Превью итоговой оценки командного решения (для преподавателя)
    /// </summary>
    [HttpPost("team-solution/{solutionId}/preview")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<GradeBreakdownDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PreviewScore(Guid solutionId, GradePreviewRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _teamSolutionService.PreviewScoreAsync(userId, solutionId, dto);
        return Ok(new ApiResponse<GradeBreakdownDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}