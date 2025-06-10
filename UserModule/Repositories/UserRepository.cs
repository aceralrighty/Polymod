using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Repositories;

internal class UserRepository(UserDbContext context) : GenericRepository<User>(context), IUserRepository
{
    public override async Task<User> GetByIdAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Id == id) ?? throw new NullReferenceException();
    }


    public async Task<User> GetByEmailAsync(string email)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Email == email) ??
               throw new InvalidOperationException($"User with email {email} not found");
    }

    public async Task<User> GetByUsernameAsync(string username)
    {
        return await DbSet.FirstOrDefaultAsync(u => u.Username == username) ??
               throw new InvalidOperationException($"User with username {username} not found");
    }

    public async Task AddRangeAsync(IEnumerable<User> entities)
    {
        await DbSet.AddRangeAsync(entities);
        await Context.SaveChangesAsync();
    }

    public async Task RemoveAsync(User entity)
    {
        DbSet.Remove(entity);
        await Context.SaveChangesAsync();
    }

    public async Task<int> GetCountAsync()
    {
        return await DbSet.CountAsync();
    }

    public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize)
    {
        return await DbSet.OrderBy(u => u.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task<IEnumerable<User>> GetTopAsync(int count)
    {
        return await DbSet.Take(count).ToListAsync();
    }
}
