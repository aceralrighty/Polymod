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
}