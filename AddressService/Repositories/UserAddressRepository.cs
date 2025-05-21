using Microsoft.EntityFrameworkCore;
using TBD.AddressService.Data;
using TBD.AddressService.Models;

namespace TBD.AddressService.Repositories;

internal class UserAddressRepository : GenericAddressRepository<UserAddress>, IUserAddressRepository
{
    protected UserAddressRepository(AddressDbContext context) : base(context)
    {
    }


    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(u =>
            u.Address1 == userAddress.Address1 && u.ZipCode == userAddress.ZipCode) ?? throw new Exception();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.State).ToListAsync();
    }

    public async Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode ?? 0).ToListAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync(UserAddress userAddress)
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync();
    }
}