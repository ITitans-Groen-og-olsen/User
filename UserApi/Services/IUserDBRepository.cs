using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UserApi.Models;

namespace UserApi.Services;

public interface IUserDBRepository
{
    Task<User> CreateUserAsync(User user);
    Task<User> GetUserByIdAsync(string id);
    Task<IEnumerable<User>> GetAllUsersAsync();
    Task<User> UpdateUserAsync(string id, User updatedUser);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> Login(User user);
    string HashPassword(User user);
}
