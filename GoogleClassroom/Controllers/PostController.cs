using Application.DTOs.Post;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.Post;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api")]
public class PostController : ControllerBase
{
    private readonly IPostService _postService;

    public PostController(IPostService postService)
    {
        _postService = postService;
    }

    /// <summary>
    /// Создать пост или задание в курсе (только для преподавателей)
    /// </summary>
    [HttpPost("course/{courseId}/task")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreatePost(Guid courseId, CreateUpdatePostDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _postService.CreatePostAsync(userId, courseId, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Получить пост или задание по ID (доступно всем участникам курса)
    /// </summary>
    [HttpGet("post/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<PostDetailsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPost(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _postService.GetPostAsync(userId, id);
        return Ok(new ApiResponse<PostDetailsDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Обновить пост или задание (только для преподавателей)
    /// </summary>
    [HttpPut("post/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePost(Guid id, CreateUpdatePostDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _postService.UpdatePostAsync(userId, id, dto);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Удалить пост или задание (только для преподавателей)
    /// </summary>
    [HttpDelete("post/{id}")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<IdRequestDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePost(Guid id)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _postService.DeletePostAsync(userId, id);
        return Ok(new ApiResponse<IdRequestDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Получить ленту постов и заданий курса с пагинацией (доступно всем участникам)
    /// </summary>
    [HttpGet("course/{courseId}/feed")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    [ProducesResponseType(typeof(ApiResponse<FeedResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCourseFeed(Guid courseId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _postService.GetCourseFeedAsync(userId, courseId, skip, take);
        return Ok(new ApiResponse<FeedResponseDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}