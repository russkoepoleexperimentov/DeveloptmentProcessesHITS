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
public class SolutionController : ControllerBase
{
    private readonly ISolutionService _solutionService;
    
    public  SolutionController(ISolutionService solutionService)
    {
        _solutionService = solutionService;
    }

    [HttpPut("task/{id}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitSolution(Guid id, SubmitSolutionRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _solutionService.SubmitSolutionAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpDelete("task/{id}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteSolution(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;

        var result = await _solutionService.DeleteSolutionAsync(userId, id);

        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpGet("task/{id}/solution")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<StudentSolutionDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolution(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;

        var result = await _solutionService.GetSolutionByIdAsync(userId, id);

        return Ok(new ApiResponse<StudentSolutionDetailsDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpGet("task/{id}/solutions")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<SolutionListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolutionList(
        Guid id,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] SolutionStatus? status = null,
        [FromQuery] Guid? studentId = null)
    {
        var userId = HttpContext.GetUserId()!.Value;

        var result = await _solutionService.GetSolutionListAsync(
            userId,
            id,
            skip,
            take,
            status,
            studentId);

        return Ok(new ApiResponse<SolutionListDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpPost("solution/{solutionId}/review")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReviewSolution(Guid solutionId, UpdateSolutionRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;

        var result = await _solutionService.MarkSolutionAsync(userId, solutionId, dto);

        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}