using Application.DTOs.Grading.PeerReview;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers;

[ApiController]
[Route("api")]
public class PeerReviewController : ControllerBase
{
    private readonly IPeerReviewService _peerReviewService;

    public PeerReviewController(IPeerReviewService peerReviewService)
    {
        _peerReviewService = peerReviewService;
    }

    [HttpGet("task/{taskId}/peer-review/next")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewTargetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetNext(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.GetNextAssignmentAsync(userId, taskId);
        return Ok(new ApiResponse<PeerReviewTargetDto?>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpPost("peer-review/{reviewId}/submit")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid reviewId, SubmitPeerReviewDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.SubmitReviewAsync(userId, reviewId, dto);
        return Ok(new ApiResponse<PeerReviewProgressDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpGet("task/{taskId}/peer-review/progress")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIndividualProgress(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.GetIndividualProgressAsync(userId, taskId);
        return Ok(new ApiResponse<PeerReviewProgressDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpPost("task/{taskId}/peer-review/finish")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Finish(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.FinishAsync(userId, taskId);
        return Ok(new ApiResponse<PeerReviewProgressDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpGet("team-task/{taskId}/peer-review/available")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<PeerReviewTeamTargetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableTeamSolutions(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.GetAvailableTeamSolutionsAsync(userId, taskId);
        return Ok(new ApiResponse<PagedResponse<PeerReviewTeamTargetDto>>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpPost("team-solution/{teamSolutionId}/peer-review")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitTeamReview(Guid teamSolutionId, SubmitPeerReviewDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.SubmitTeamReviewAsync(userId, teamSolutionId, dto);
        return Ok(new ApiResponse<PeerReviewProgressDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    [HttpGet("team-task/{taskId}/peer-review/progress")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PeerReviewProgressDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTeamProgress(Guid taskId)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _peerReviewService.GetTeamProgressAsync(userId, taskId);
        return Ok(new ApiResponse<PeerReviewProgressDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}
