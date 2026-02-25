using GoogleClass.DTOs.Auth;
using GoogleClass.Models;

namespace Application.Services.Abstractions
{
    public interface IJwtService
    {
        TokenResponse GenerateTokens(User user);
    }
}