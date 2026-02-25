using Application.DTOs.Auth;
using Application.Services.Interfaces;
using Common;
using GoogleClass.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegisterDto dto)
    {
        return Ok(await _authService.RegisterAsync(dto));
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var id = HttpContext.GetUserId();

        await _authService.LogoutAsync(id.Value);

        return Ok();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(UserLoginDto dto)
    {
        return Ok(await _authService.LoginAsync(dto));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(string token)
    {
        return Ok(await _authService.RefreshAsync(token));
    }

    [Authorize(AuthenticationSchemes = "Bearer")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(UserChangePassword dto)
    {
        var userId = Guid.Parse(
            User.FindFirst("nameid")!.Value);

        await _authService.ChangePasswordAsync(userId, dto);

        return Ok();
    }
}