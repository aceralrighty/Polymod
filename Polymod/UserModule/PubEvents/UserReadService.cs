using Microsoft.EntityFrameworkCore;
using PolyMod.API.DTOs.Users;
using PolyMod.Shared.Events.Interfaces;
using PolyMod.UserModule.Data;

namespace PolyMod.UserModule.PubEvents;

public class UserReadService(UserDbContext context) : IUserReadService
{
    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await context.Users.FindAsync(userId);
        return user == null
            ? null
            : new UserDto(user.Id, user.Email ?? throw new InvalidOperationException("User email is null"));
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email);

        return user == null
            ? null
            : new UserDto(user.Id, user.Email ?? throw new InvalidOperationException("User email is null"))
            {
                Username = user.Username
            };
    }

    public async Task<List<UserDto?>> GetAllUsersAsync()
    {
        return (await context.Users
            .Select(u => new UserDto(u.Id, u.Email ?? string.Empty)
            {
                Username = u.Username
            })
            .ToListAsync())!;
    }
}
