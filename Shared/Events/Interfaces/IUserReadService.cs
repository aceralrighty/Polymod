using TBD.API.DTOs.Users;

namespace TBD.Shared.Events.Interfaces;

public interface IUserReadService
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<List<UserDto?>> GetAllUsersAsync();
}
