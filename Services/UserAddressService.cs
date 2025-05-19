using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Models;
using TBD.Repository;

namespace TBD.Services;

public class UserAddressService : IGenericRepository<UserAddress>, IUserAddressService
{
    protected readonly GenericDatabaseContext _context;
    protected readonly DbSet<UserAddress>? _dbSet;


    public async Task<List<IGrouping<string, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.State).ToListAsync();
    }

    public async Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode ?? 0).ToListAsync();
    }

    public async Task<List<IGrouping<string, UserAddress>>> GroupByCityAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync();
    }

    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(ua =>
            ua.Address1 == userAddress.Address1 || ua.Address2 == userAddress.Address2);
    }

    public async Task<IEnumerable<UserAddress>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public async Task<IEnumerable<UserAddress>> FindAsync(Expression<Func<UserAddress, bool>> expression)
    {
        return await _dbSet.Where(expression).ToListAsync();
    }

    public async Task<UserAddress> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.Id == id).WaitAsync(TimeSpan.FromSeconds(30));
    }

    public async Task AddAsync(UserAddress entity)
    {
        await _dbSet.AddAsync(entity);
    }

    public async Task AddRangeAsync(IEnumerable<UserAddress> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public void Update(UserAddress entity)
    {
        _dbSet.Update(entity);
    }

    public void Remove(UserAddress entity)
    {
        _dbSet.Remove(entity);
    }
}