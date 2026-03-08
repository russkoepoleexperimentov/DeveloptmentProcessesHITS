using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Common;
using GoogleClass.DTOs.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/users")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;

    public UserController(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Получить пользователя по идентификатору
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _userService.GetByIdAsync(id);
        return Ok(new ApiResponse<UserDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Обновить данные текущего пользователя
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSelf(UserUpdateDto dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        await _userService.UpdateAsync(userId, dto);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = new { }
        });
    }

    /// <summary>
    /// Получить информацию о текущем пользователе
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMe()
    {
        var userId = HttpContext.GetUserId()!.Value;
        var result = await _userService.GetByIdAsync(userId);
        return Ok(new ApiResponse<UserDto>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Поиск пользователей по строке запроса
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<UserDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchUsers(string? query)
    {
        var result = await _userService.GetAllUsersAsync(query);
        return Ok(new ApiResponse<List<UserDto>>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }
}