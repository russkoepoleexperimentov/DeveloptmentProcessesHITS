using AutoMapper;
using Common.Exceptions;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using GoogleClass.DTOs.User;
using GoogleClass.Models;
using Microsoft.AspNetCore.Identity;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public class UserServiceTests
{
    private readonly Mock<UserManager<User>> _userManager;
    private readonly Mock<IValidator<UserUpdateDto>> _validator;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManager;

    private readonly UserService _service;

    public UserServiceTests()
    {
        var store = new Mock<IUserStore<User>>();
        _userManager = new Mock<UserManager<User>>(
            store.Object, null, null, null, null, null, null, null, null);

        var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
        _roleManager = new Mock<RoleManager<IdentityRole<Guid>>>(
            roleStore.Object, null, null, null, null);

        _validator = new Mock<IValidator<UserUpdateDto>>();
        _mapper = new Mock<IMapper>();

        _service = new UserService(
            _userManager.Object,
            _mapper.Object,
            _validator.Object,
            _roleManager.Object);
    }


    [Fact]
    public async Task GetByIdAsync_ShouldThrow_WhenUserNotFound()
    {
        _userManager.Setup(x => x.FindByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((User)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _service.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnMappedUser()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var dto = new UserDto { Id = userId };

        _userManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _mapper.Setup(x => x.Map<UserDto>(user))
            .Returns(dto);

        var result = await _service.GetByIdAsync(userId);

        result.Id.Should().Be(userId);
    }

    [Fact]
    public async Task UpdateAsync_ShouldMapAndCallUpdate()
    {
        var userId = Guid.NewGuid();
        var user = new User { Id = userId };
        var dto = new UserUpdateDto();

        _userManager.Setup(x => x.FindByIdAsync(userId.ToString()))
            .ReturnsAsync(user);

        _userManager.Setup(x => x.UpdateAsync(user))
            .ReturnsAsync(IdentityResult.Success);

        await _service.UpdateAsync(userId, dto);

        _mapper.Verify(x => x.Map(dto, user), Times.Once);
        _userManager.Verify(x => x.UpdateAsync(user), Times.Once);
    }
}