using PolyMod.AuthModule.Models;

namespace PolyMod.AuthModule.Repositories;

public interface IAuthRepository
{
    Task<IEnumerable<AuthUser>> GetAllUsers();
    Task<AuthUser?> GetUserById(Guid id);
    Task<AuthUser?> GetUserByUsername(string username);
    Task<AuthUser?> GetUserByEmail(string email);

    Task<IEnumerable<AuthUser>> GetUsersByLoginAttempts(int loginAttempts);

    Task<AuthUser?> GetUserByRefreshToken(string refreshToken);
    Task InvalidateRefreshTokenAsync(Guid userId);

    Task AddAsync(AuthUser entity);
    Task UpdateAsync(AuthUser entity);
}
