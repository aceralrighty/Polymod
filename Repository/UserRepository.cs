using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Models;

namespace TBD.Repository;

public class UserRepository(GenericDatabaseContext context) : GenericRepository<User>(context), IUserRepository
{
    public new async Task<IEnumerable<User>> GetAllAsync()
    {
        // Custom implementation or just call base
        return await _dbSet.ToListAsync() ?? throw new InvalidOperationException("No users found");
    }


    // Implement the methods from IUserRepository that don't exist in GenericRepository
    public async Task<User> GetByEmailAsync(string email)
    {
        // Now you can use _dbSet from the base class
        return await _dbSet.FirstOrDefaultAsync(u => u.Email == email) ??
               throw new InvalidOperationException($"User with email {email} not found");
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await _dbSet.FirstOrDefaultAsync(u => u.Username == username) ??
               throw new InvalidOperationException($"User with username {username} not found");
    }
}