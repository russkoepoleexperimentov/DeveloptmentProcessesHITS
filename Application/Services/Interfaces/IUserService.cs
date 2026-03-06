
using GoogleClass.DTOs.User;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Services.Interfaces
{
    public interface IUserService
    {
        Task<UserDto> GetByIdAsync(Guid userId);
        Task UpdateAsync(Guid userId, UserUpdateDto request);
        Task<List<UserDto>> GetAllUsersAsync(string? query);
    }
}
