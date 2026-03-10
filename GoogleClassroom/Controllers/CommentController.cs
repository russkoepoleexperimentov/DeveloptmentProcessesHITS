using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GoogleClassroom.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class CommentController : ControllerBase
{
    private readonly ICommentService _commentService;

    public CommentController(ICommentService commentService)
    {
        _commentService = commentService;
    }

    [HttpPost("post/{id}/comment")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePostComment(Guid id, AddCommentRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.CreatePostCommentAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpPost("solution/{id}/comment")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateSolutionComment(Guid id, AddCommentRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.CreateSolutionCommentAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpGet("post/{id}/comment")]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPostRootComment(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.GetPostRootCommentAsync(userId, id);
        return Ok(new ApiResponse<List<CommentDto>>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpGet("solution/{id}/comment")]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSolutionRootComment(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.GetSolutionRootCommentAsync(userId, id);
        return Ok(new ApiResponse<List<CommentDto>>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpGet("comment/{id}/replies")]
    [ProducesResponseType(typeof(ApiResponse<List<CommentDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCommentReplies(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.GetCommentRepliesAsync(userId, id);
        return Ok(new ApiResponse<List<CommentDto>>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpPost("comment/{id}/reply")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateCommentReplyComment(Guid id, AddCommentRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.CreateCommentReplyAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpPut("comment/{id}")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EditComment(Guid id, EditCommentRequestDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.EditCommentAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
    
    [HttpDelete("comment/{id}")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteComment(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _commentService.DeleteCommentAsync(userId, id);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}