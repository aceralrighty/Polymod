using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.Shared.Repositories;

namespace TBD.AuthModule.Repositories;

public class AuthRepository(AuthDbContext context) : GenericRepository<AuthUser>(context), IAuthRepository
{
    private readonly AuthDbContext _context = context;

    public async Task<IEnumerable<AuthUser>> GetAllUsers()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<AuthUser?> GetUserById(Guid id)
    {
        var found = await _dbSet.FindAsync(id);
        return found;
    }

    public async Task<AuthUser?> GetUserByUsername(string username)
    {
        var found = await _dbSet.FirstOrDefaultAsync(u => u.Username == username);
        return found;
    }

    public async Task<AuthUser?> GetUserByEmail(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<AuthUser>> GetUsersByLoginAttempts(int loginAttempts)
    {
        var foundByAttempts = _dbSet.FirstOrDefaultAsync(ua => ua.FailedLoginAttempts == loginAttempts);
        return await _dbSet.ToListAsync();
    }

    public async Task<AuthUser?> GetUserByRefreshToken(string refreshToken)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);
    }

    public async Task InvalidateRefreshTokenAsync(Guid userId)
    {
        var user = await _context.AuthUsers.FirstOrDefaultAsync(id => id.Id == userId);
        if (user != null)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            await _context.SaveChangesAsync();
        }
    }
}
