using TBD.AuthModule.Models;

namespace TBD.AuthModule.Repositories;

internal interface IAuthRepository
{
    Task<IEnumerable<AuthUser>> GetAllUsers();
    Task<AuthUser?> GetUserById(Guid id);
    Task<AuthUser?> GetUserByUsername(string username);
    Task<AuthUser?> GetUserByEmail(string email);

    Task<IEnumerable<AuthUser>> GetUsersByLoginAttempts(int loginAttempts);
}
