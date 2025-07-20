using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.Users;
using TBD.Shared.Events.Interfaces;
using TBD.UserModule.Data;

namespace TBD.UserModule.PubEvents;

public class UserReadService(UserDbContext context) : IUserReadService
{
    private readonly UserDbContext _context = context;

    public async Task<UserDto?> GetUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user == null ? null : new UserDto(user.Id, user.Email);
    }

    public async Task<List<UserDto?>> GetAllUsersAsync()
    {
        return await _context.Users.Select(u => new UserDto(u.Id, u.Email)).ToListAsync();
    }
}
