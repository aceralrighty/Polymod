using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.Shared.Repositories;

namespace TBD.AuthModule.Repositories;

internal class AuthRepository(AuthDbContext context) : GenericRepository<AuthUser>(context), IAuthRepository
{
    public async Task<IEnumerable<AuthUser>> GetAllUsers()
    {
        return await DbSet.ToListAsync();
    }

    public async Task<AuthUser?> GetUserById(Guid id)
    {
        var found = await DbSet.FindAsync(id);
        return found;
    }

    public async Task<AuthUser?> GetUserByUsername(string username)
    {
        var found = await DbSet.FirstOrDefaultAsync(u => u.Username == username);
        return found;
    }

    public async Task<AuthUser?> GetUserByEmail(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<AuthUser>> GetUsersByLoginAttempts(int loginAttempts)
    {
        return await DbSet.Where(ua => ua.FailedLoginAttempts == loginAttempts).ToListAsync();
    }

    public async Task<AuthUser?> GetUserByRefreshToken(string refreshToken)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task InvalidateRefreshTokenAsync(Guid userId)
    {
        var user = await context.AuthUsers.FirstOrDefaultAsync(id => id.Id == userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await context.SaveChangesAsync();
        }
    }
}
