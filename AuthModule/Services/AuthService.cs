using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.AuthModule.Repositories;
using TBD.Shared.Utils;

namespace TBD.AuthModule.Services;

internal class AuthService(AuthDbContext context) : IAuthService, IAuthRepository
{
    protected readonly AuthDbContext _dbContext;
    private readonly DbSet<AuthUser> _dbSet = context.Set<AuthUser>();

    public async Task<AuthUser?> AuthenticateAsync(string username, string password)
    {
        var authUser = await GetAuthUserByUsernameAsync(username);
        if (authUser == null) return null;
        if (Hasher.Verify(authUser.HashedPassword, password))
        {
            authUser.LastLogin = DateTime.UtcNow;
            authUser.FailedLoginAttempts = 0;
            await _dbContext.SaveChangesAsync();
            return authUser;
        }

        authUser.FailedLoginAttempts++;
        await _dbContext.SaveChangesAsync();
        return null;
    }

    public async Task<AuthUser?> GetAuthUserByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(au => au.Username == username);
    }

    public Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }
}
