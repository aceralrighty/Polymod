using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PolyMod.AddressModule.Data;
using PolyMod.AddressModule.Models;
using PolyMod.Shared.Repositories;

namespace PolyMod.AddressModule.Repositories;

internal class UserAddressRepository(AddressDbContext context)
    : GenericRepository<UserAddress>(context), IUserAddressRepository
{
    private readonly DbSet<UserAddress> _dbSet = context.Set<UserAddress>();

    // Basic CRUD operations needed by gRPC service
    public new async Task<UserAddress?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<UserAddress> CreateAsync(UserAddress userAddress)
    {
        if (userAddress == null)
            throw new ArgumentNullException(nameof(userAddress), "The address entity cannot be null.");
        await AddAsync(userAddress);
        return userAddress;
    }
    public new async Task<IEnumerable<UserAddress>> FindAsync(Expression<Func<UserAddress, bool>> expression)
    {

        return await _dbSet.Where(expression).ToListAsync();
    }

    public override async Task<UserAddress> UpdateAsync(UserAddress userAddress)
    {
        ArgumentNullException.ThrowIfNull(userAddress);
        _dbSet.Update(userAddress);
        await context.SaveChangesAsync();
        return userAddress;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var entity = await GetByIdAsync(id);
        if (entity == null) return false;

        await base.DeleteAsync(entity);
        return true;
    }

    public async Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId)
    {
        return await _dbSet.Where(ua => ua.UserId == userId).ToListAsync();
    }

    // Existing custom methods (keep these for backward compatibility)
    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(ua =>
                   ua.Address1 == userAddress.Address1 || ua.Address2 == userAddress.Address2) ??
               throw new InvalidOperationException();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync()
    {
        var data = await _dbSet.ToListAsync();
        return data.GroupBy(ua => ua.State).ToList();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync()
    {
        var data = await _dbSet.ToListAsync();
        return data.GroupBy(ua => ua.ZipCode).ToList();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync()
    {
        var data = await _dbSet.ToListAsync();
        return data.GroupBy(ua => ua.City).ToList();
    }
}
