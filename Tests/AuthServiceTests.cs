using Application.DTOs.Auth;
using Application.Services.Abstractions;
using AutoMapper;
using Common.Exceptions;
using Common.Options;
using Domain.Models;
using FluentAssertions;
using FluentValidation;
using GoogleClass.DTOs.Auth;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests
{
    public class AuthServiceTests
    {
        private readonly Mock<UserManager<User>> _userManager;
        private readonly Mock<IJwtService> _jwtService;
        private readonly Mock<IValidator<UserRegisterDto>> _registerValidator;
        private readonly Mock<IValidator<UserLoginDto>> _loginValidator;
        private readonly Mock<IValidator<UserChangePassword>> _changeValidator;
        private readonly Mock<IMapper> _mapper;

        private readonly GcDbContext _context;
        private readonly AuthService _service;

        public AuthServiceTests()
        {
            var store = new Mock<IUserStore<User>>();
            _userManager = new Mock<UserManager<User>>(
                store.Object, null, null, null, null, null, null, null, null);

            _jwtService = new Mock<IJwtService>();
            _registerValidator = new Mock<IValidator<UserRegisterDto>>();
            _loginValidator = new Mock<IValidator<UserLoginDto>>();
            _changeValidator = new Mock<IValidator<UserChangePassword>>();
            _mapper = new Mock<IMapper>();

            var options = new DbContextOptionsBuilder<GcDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new GcDbContext(options);

            var jwtOptions = Options.Create(new JwtOptions
            {
                Refresh = new RefreshOptions { LifetimeDays = 7 }
            });

            _service = new AuthService(
                _userManager.Object,
                _context,
                _jwtService.Object,
                _registerValidator.Object,
                _loginValidator.Object,
                _changeValidator.Object,
                _mapper.Object,
                jwtOptions);
        }

        [Fact]
        public async Task RegisterAsync_ShouldThrow_WhenUserExists()
        {
            var dto = new UserRegisterDto { Email = "a@mail.com" };

            _userManager.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(new User());

            await Assert.ThrowsAsync<EntryExistsException>(
                () => _service.RegisterAsync(dto));
        }

        [Fact]
        public async Task RegisterAsync_ShouldCreateUser_AndSaveRefreshToken()
        {
            var dto = new UserRegisterDto { Email = "a@mail.com", Password = "123" };
            var user = new User { Id = Guid.NewGuid() };

            _userManager.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((User)null);

            _mapper.Setup(x => x.Map<User>(dto)).Returns(user);

            _userManager.Setup(x => x.CreateAsync(user, dto.Password))
                .ReturnsAsync(IdentityResult.Success);

            _userManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            _jwtService.Setup(x => x.GenerateTokens(user))
                .Returns(new TokenResponse
                {
                    AccessToken = "access",
                    RefreshToken = "refresh"
                });

            var result = await _service.RegisterAsync(dto);

            result.AccessToken.Should().Be("access");
            _context.RefreshTokens.Should().HaveCount(1);
        }

        [Fact]
        public async Task LoginAsync_ShouldThrow_WhenUserNotFound()
        {
            var dto = new UserLoginDto { Email = "x", Password = "123" };

            _userManager.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<BadRequestException>(
                () => _service.LoginAsync(dto));
        }


        [Fact]
        public async Task LoginAsync_ShouldGenerateTokens_AndSaveRefresh()
        {
            var user = new User { Id = Guid.NewGuid() };
            var dto = new UserLoginDto { Email = "x", Password = "123" };

            _userManager.Setup(x => x.FindByEmailAsync(dto.Email))
                .ReturnsAsync(user);

            _userManager.Setup(x => x.CheckPasswordAsync(user, dto.Password))
                .ReturnsAsync(true);

            _userManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            _jwtService.Setup(x => x.GenerateTokens(user))
                .Returns(new TokenResponse
                {
                    AccessToken = "a",
                    RefreshToken = "r"
                });

            var result = await _service.LoginAsync(dto);

            result.RefreshToken.Should().Be("r");
            _context.RefreshTokens.Should().HaveCount(1);
        }

        [Fact]
        public async Task RefreshAsync_ShouldThrow_WhenTokenInvalid()
        {
            await Assert.ThrowsAsync<SecurityTokenException>(
                () => _service.RefreshAsync("bad"));
        }


        [Fact]
        public async Task RefreshAsync_ShouldRevokeOld_AndCreateNew()
        {
            var user = new User { Id = Guid.NewGuid() };

            var oldToken = new RefreshToken
            {
                Token = "old",
                UserId = user.Id,
                Expiration = DateTime.UtcNow.AddDays(1)
            };

            _context.RefreshTokens.Add(oldToken);
            await _context.SaveChangesAsync();

            _userManager.Setup(x => x.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);

            _userManager.Setup(x => x.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            _jwtService.Setup(x => x.GenerateTokens(user))
                .Returns(new TokenResponse
                {
                    AccessToken = "newA",
                    RefreshToken = "newR"
                });

            var result = await _service.RefreshAsync("old");

            oldToken.IsRevoked.Should().BeTrue();
            _context.RefreshTokens.Should().HaveCount(2);
            result.RefreshToken.Should().Be("newR");
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldThrow_WhenUserNotFound()
        {
            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((User)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.ChangePasswordAsync(Guid.NewGuid(),
                    new UserChangePassword()));
        }

        [Fact]
        public async Task ChangePasswordAsync_ShouldSucceed_WhenValid()
        {
            var user = new User();

            _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
                .ReturnsAsync(user);

            _userManager.Setup(x =>
                x.ChangePasswordAsync(user, It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(IdentityResult.Success);

            await _service.ChangePasswordAsync(Guid.NewGuid(),
                new UserChangePassword
                {
                    OldPassword = "old",
                    NewPassword = "new"
                });
        }
    }
}
