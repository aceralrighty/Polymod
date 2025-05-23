using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;

namespace TBD.AddressModule.Repositories;

internal class UserAddressRepository : GenericAddressRepository<UserAddress>, IUserAddressRepository
{
    public UserAddressRepository(AddressDbContext context) : base(context)
    {
    }


    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await _dbSet.FirstOrDefaultAsync(u =>
            u.Address1 == userAddress.Address1 && u.ZipCode == userAddress.ZipCode) ?? throw new Exception();
    }

    public async Task<List<IGrouping<string, UserAddress>>> GroupByUserStateAsync()
    {
        return (await _dbSet.GroupBy(ua => ua.State).ToListAsync())!;
    }

    public async Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync()
    {
        return await _dbSet.GroupBy(ua => ua.ZipCode).ToListAsync();
    }

    public async Task<List<IGrouping<string, UserAddress>>> GroupByCityAsync()
    {
        return await _dbSet.GroupBy(ua => ua.City).ToListAsync();
    }
}