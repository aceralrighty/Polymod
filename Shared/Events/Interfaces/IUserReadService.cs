using TBD.API.DTOs.Users;

namespace TBD.Shared.Events.Interfaces;

internal interface IUserReadService
{
    Task<UserDto?> GetUserAsync(Guid userId);
    Task<List<UserDto?>> GetAllUsersAsync();
}
