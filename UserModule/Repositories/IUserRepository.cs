using System.Linq.Expressions;
using TBD.UserModule.Models;

namespace TBD.UserModule.Repositories;

internal interface IUserRepository
{
    Task<User> GetByEmailAsync(string email);
    Task<User> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> expression);
    Task<User> GetByIdAsync(Guid id);
    Task AddAsync(User entity);
    Task AddRangeAsync(IEnumerable<User> entities);
    Task UpdateAsync(User entity);
    Task RemoveAsync(User entity);
    Task<int> GetCountAsync();
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<User>> GetTopAsync(int count);
}
