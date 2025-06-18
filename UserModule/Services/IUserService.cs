using TBD.API.DTOs;
using TBD.API.DTOs.Users;

namespace TBD.UserModule.Services;

public interface IUserService
{
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto?> GetUserByUsernameAsync(string username);

    Task<PagedResult<UserDto>> GetUsersAsync(int page = 1, int pageSize = 50);

    // Keep GetAllUsersAsync but mark as obsolete for safety
    [Obsolete("Use GetUsersAsync with pagination to avoid memory issues")]
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    Task CreateUserAsync(UserDto? user);
    Task UpdateUserAsync(UserDto? user);
    Task DeleteUserAsync(Guid id);
}
