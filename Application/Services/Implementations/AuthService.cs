using Application.DTOs.Auth;
using Application.Services.Abstractions;
using Application.Services.Interfaces;
using AutoMapper;
using Common.Exceptions;
using Common.Options;
using Domain.Models;
using FluentValidation;
using GoogleClass.DTOs.Auth;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class AuthService : IAuthService
{
    private readonly UserManager<User> _userManager;
    private readonly GcDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IValidator<UserRegisterDto> _registerValidator;
    private readonly IValidator<UserLoginDto> _loginValidator;
    private readonly IValidator<UserChangePassword> _changeValidator;
    private readonly IMapper _mapper;
    private readonly JwtOptions _jwtOptions;

    public AuthService(
        UserManager<User> userManager,
        GcDbContext context,
        IJwtService jwtService,
        IValidator<UserRegisterDto> registerValidator,
        IValidator<UserLoginDto> loginValidator,
        IValidator<UserChangePassword> changeValidator,
        IMapper mapper,
        IOptions<JwtOptions> jwtOptions)
    {
        _userManager = userManager;
        _context = context;
        _jwtService = jwtService;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _changeValidator = changeValidator;
        _mapper = mapper;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<TokenResponse> RegisterAsync(UserRegisterDto dto)
    {
        await _registerValidator.ValidateAndThrowAsync(dto);

        if (await _userManager.FindByEmailAsync(dto.Email) != null)
            throw new EntryExistsException($"User with Email {dto.Email} already exists.");

        var user = _mapper.Map<User>(dto);
        user.UserName = $"user_{Guid.NewGuid()}";

        var result = await _userManager.CreateAsync(user, dto.Password);

        if (!result.Succeeded)
            throw new BadRequestException(
                string.Join(", ", result.Errors.Select(x => x.Description)));

        var roles = await _userManager.GetRolesAsync(user);

        var tokens = _jwtService.GenerateTokens(user);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.RefreshToken,
            Expiration = DateTime.UtcNow.AddDays(_jwtOptions.Refresh.LifetimeDays)
        });

        await _context.SaveChangesAsync();

        return tokens;
    }

    public async Task<TokenResponse> LoginAsync(UserLoginDto dto)
    {
        await _loginValidator.ValidateAndThrowAsync(dto);

        var user = await _userManager.FindByEmailAsync(dto.Email);

        if (user == null ||
            !await _userManager.CheckPasswordAsync(user, dto.Password))
            throw new BadRequestException("Wrong login or password");

        var roles = await _userManager.GetRolesAsync(user);

        var tokens = _jwtService.GenerateTokens(user);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.RefreshToken,
            Expiration = DateTime.UtcNow.AddDays(_jwtOptions.Refresh.LifetimeDays)
        });

        await _context.SaveChangesAsync();

        return tokens;
    }

    public async Task<TokenResponse> RefreshAsync(string refreshToken)
    {
        var tokenEntity = await _context.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken);

        if (tokenEntity == null ||
            tokenEntity.IsRevoked ||
            tokenEntity.Expiration < DateTime.UtcNow)
            throw new SecurityTokenException("Invalid token");

        tokenEntity.IsRevoked = true;

        var user = await _userManager.FindByIdAsync(tokenEntity.UserId.ToString())
            ?? throw new NotFoundException("User not found");

        var roles = await _userManager.GetRolesAsync(user);

        var tokens = _jwtService.GenerateTokens(user);

        _context.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = tokens.RefreshToken,
            Expiration = DateTime.UtcNow.AddDays(_jwtOptions.Refresh.LifetimeDays)
        });

        await _context.SaveChangesAsync();

        return tokens;
    }

    public async Task LogoutAsync(Guid userId)
    {
        var tokens = await _context.RefreshTokens
            .Where(x => x.UserId == userId && !x.IsRevoked)
            .ToListAsync();

        foreach (var token in tokens)
            token.IsRevoked = true;

        await _context.SaveChangesAsync();
    }

    public async Task ChangePasswordAsync(Guid userId, UserChangePassword dto)
    {
        await _changeValidator.ValidateAndThrowAsync(dto);

        var user = await _userManager.FindByIdAsync(userId.ToString())
            ?? throw new NotFoundException("User not found");

        var result = await _userManager.ChangePasswordAsync(
            user,
            dto.OldPassword,
            dto.NewPassword);

        if (!result.Succeeded)
            throw new BadRequestException(
                string.Join(", ", result.Errors.Select(e => e.Description)));
    }
}