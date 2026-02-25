using Application.DTOs.Auth;
using GoogleClass.DTOs.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task<TokenResponse> RegisterAsync(UserRegisterDto dto);
        Task<TokenResponse> LoginAsync(UserLoginDto dto);
        Task<TokenResponse> RefreshAsync(string refreshToken);
        Task LogoutAsync(Guid userId);
        Task ChangePasswordAsync(Guid userId, UserChangePassword dto);
    }
}
