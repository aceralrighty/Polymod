using PolyMod.API.DTOs.Users;

namespace PolyMod.Shared.Events.Interfaces;

public interface IUserReadService
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<List<UserDto?>> GetAllUsersAsync();
}
