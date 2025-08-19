using Microsoft.EntityFrameworkCore;
using PolyMod.Shared.Repositories;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;

namespace TBD.AddressModule.Repositories;

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

        var result = await _dbSet.AddAsync(userAddress);
        await context.SaveChangesAsync();
        return result.Entity;
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

        _dbSet.Remove(entity);
        await context.SaveChangesAsync();
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
        return await _dbSet.GroupBy(ua => ua.State).ToListAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync()
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode).ToListAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync()
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync();
    }
}
