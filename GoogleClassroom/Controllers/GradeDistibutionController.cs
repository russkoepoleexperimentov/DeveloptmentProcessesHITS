// GoogleClassroom/Controllers/GradeDistributionController.cs
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.GradeDistribution;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers;

[ApiController]
[Route("api/teams/{teamId}/assignments/{assignmentId}/grade-distribution")]
public class GradeDistributionController : ControllerBase
{
    private readonly IGradeDistributionService _service;

    public GradeDistributionController(IGradeDistributionService service)
    {
        _service = service;
    }

    /// <summary>Получить текущее распределение оценок</summary>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<GradeDistributionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDistribution(Guid teamId, Guid assignmentId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _service.GetDistributionAsync(teamId, assignmentId, userId);
        return Ok(new ApiResponse<GradeDistributionResponseDto>
        {
            Type = ApiResponseType.Success,
            Data = result
        });
    }

    /// <summary>Изменить распределение (только капитан)</summary>
    [HttpPut]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<GradeDistributionResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateDistribution(Guid teamId, Guid assignmentId, [FromBody] GradeDistributionUpdateRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _service.UpdateDistributionAsync(teamId, assignmentId, userId, dto);
        return Ok(new ApiResponse<GradeDistributionResponseDto>
        {
            Type = ApiResponseType.Success,
            Data = result
        });
    }

    /// <summary>Проголосовать за распределение</summary>
    [HttpPost("vote")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CastVote(Guid teamId, Guid assignmentId, [FromBody] GradeDistributionVoteRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        await _service.CastVoteAsync(teamId, assignmentId, userId, dto);
        return NoContent();
    }
}