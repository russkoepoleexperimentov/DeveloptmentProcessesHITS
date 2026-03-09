using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Регистрация нового пользователя
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register(UserRegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Выход из системы (инвалидация токена)
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPost("logout")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        var userId = HttpContext.GetUserId()!.Value;
        await _authService.LogoutAsync(userId);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = new { }
        });
    }

    /// <summary>
    /// Вход в систему
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Обновление токена доступа
    /// </summary>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(string token)
    {
        var result = await _authService.RefreshAsync(token);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = result
        });
    }

    /// <summary>
    /// Смена пароля (для авторизованного пользователя)
    /// </summary>
    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPost("change-password")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(UserChangePassword dto)
    {
        var userId = HttpContext.GetUserId()!.Value;
        await _authService.ChangePasswordAsync(userId, dto);
        return Ok(new ApiResponse<object>
        {
            Type = ApiResponseType.Success,
            Message = null,
            Data = new { }
        });
    }
}