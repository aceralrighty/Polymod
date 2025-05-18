using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Models;

namespace TBD.Repository;

public class UserAddressRepository : GenericRepository<UserAddress>, IUserAddressRepository
{
    protected UserAddressRepository(GenericDatabaseContext context) : base(context)
    {
    }

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
}