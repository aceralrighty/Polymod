using PolyMod.UserModule.Models;

namespace PolyMod.UserModule.Repositories;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByUsernameAsync(string username);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User?> GetByIdAsync(Guid id);
    Task<List<User>> GetByIdsAsync(List<Guid> ids);
    Task<User> CreateAsync(User entity);
    Task AddAsync(User entity);
    Task AddRangeAsync(IEnumerable<User> entities);
    Task<User> UpdateAsync(User entity);
    Task RemoveAsync(User? entity);
    Task<int> GetCountAsync();
    Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize);
    Task<IEnumerable<User>> GetTopAsync(int count);
}
