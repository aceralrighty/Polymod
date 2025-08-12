using TBD.API.DTOs.Users;

namespace TBD.Shared.Events.Interfaces;

public interface IUserReadService
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<List<UserDto?>> GetAllUsersAsync();
}
