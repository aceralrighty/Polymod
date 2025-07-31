using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.Users;
using TBD.Shared.Events.Interfaces;
using TBD.UserModule.Data;

namespace TBD.UserModule.PubEvents;

public class UserReadService(UserDbContext context) : IUserReadService
{
    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user == null
            ? null
            : new UserDto(user.Id, user.Email ?? throw new InvalidOperationException("something went wrong here"));
    }

    public async Task<List<UserDto?>> GetAllUsersAsync()
    {
        return await context.Users.Select(u => new UserDto(u.Id, u.Email)).ToListAsync();
    }
}
