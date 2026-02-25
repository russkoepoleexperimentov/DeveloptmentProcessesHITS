using Application.Services.Abstractions;
using GoogleClass.DTOs.Auth;
using GoogleClass.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Application.Services.Implementations
{
    public class JWTService : IJwtService
    {
        private readonly int _accessTokenLifetimeHours;
        private readonly int _refreshTokenLifetimeDays;
        private readonly SymmetricSecurityKey _accessTokenSecretKey;
        private readonly SymmetricSecurityKey _refreshTokenSecretKey;

        public JWTService(SymmetricSecurityKey accessTokenSecretKey, int accessTokenLifetimeHours,
                          SymmetricSecurityKey refreshTokenSecretKey, int refreshTokenLifetimeDays)
        {
            _accessTokenSecretKey = accessTokenSecretKey;
            _accessTokenLifetimeHours = accessTokenLifetimeHours;
            _refreshTokenSecretKey = refreshTokenSecretKey;
            _refreshTokenLifetimeDays = refreshTokenLifetimeDays;
        }

        public TokenResponse GenerateTokens(User user)
        {
            var accessToken = GenerateAccessToken(user);
            var refreshToken = GenerateRefreshToken(user);

            var responce = new TokenResponse
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            };
            return responce;
        }

        private string GenerateAccessToken(User user)
        {

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            };

            var handler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(_accessTokenLifetimeHours),
                SigningCredentials = new SigningCredentials(_accessTokenSecretKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }

        private string GenerateRefreshToken(User user)
        {
            var handler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim("refresh_token", "true")
                }),
                Expires = DateTime.UtcNow.AddDays(_refreshTokenLifetimeDays),
                SigningCredentials = new SigningCredentials(_refreshTokenSecretKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = handler.CreateToken(tokenDescriptor);
            return handler.WriteToken(token);
        }
    }
}