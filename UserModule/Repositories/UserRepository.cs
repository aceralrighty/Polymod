using Microsoft.EntityFrameworkCore;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Repositories;

internal class UserRepository(UserDbContext context) : GenericUserRepository<User>(context), IUserRepository
{
    public async Task<User> GetByEmailAsync(string email)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email) ??
               throw new InvalidOperationException($"User with email {email} not found");
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username) ??
               throw new InvalidOperationException($"User with username {username} not found");
    }

    public async Task<int> GetCountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize)
    {
        return await _context.Users.OrderBy(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetTopAsync(int count)
    {
        return await _context.Users.Take(count).ToListAsync();
    }
}
