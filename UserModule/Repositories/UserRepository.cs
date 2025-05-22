using Microsoft.EntityFrameworkCore;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Repositories;

internal class UserRepository : GenericUserRepository<User>, IUserRepository
{
    public UserRepository(UserDbContext context) : base(context)
    {
    }
    

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
}