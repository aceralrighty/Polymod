using System.Linq.Expressions;
using TBD.AuthModule.Models;

namespace TBD.AuthModule.Repositories;

internal interface IAuthRepository
{
    Task<IEnumerable<AuthUser>> GetAllUsers();
    Task<AuthUser?> GetUserById(Guid id);
    Task<AuthUser?> GetUserByUsername(string username);
    Task<AuthUser?> GetUserByEmail(string email);

    Task<IEnumerable<AuthUser>> GetUsersByLoginAttempts(int loginAttempts);
    Task<IEnumerable<AuthUser>> GetAllAsync();
    Task<IEnumerable<AuthUser>> FindAsync(Expression<Func<AuthUser, bool>> predicate);
    Task AddAsync(AuthUser entity);
    Task UpdateAsync(AuthUser entity);
    Task DeleteAsync(AuthUser entity);
}
