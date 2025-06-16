using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.Shared.Repositories;

namespace TBD.AddressModule.Repositories;

internal class UserAddressRepository(AddressDbContext context)
    : GenericRepository<UserAddress>(context), IUserAddressRepository
{
    public async Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress)
    {
        return await DbSet.FirstOrDefaultAsync(u =>
            u.Address1 == userAddress.Address1 && u.ZipCode == userAddress.ZipCode) ?? throw new Exception();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync()
    {
        return (await DbSet.GroupBy(ua => ua.State).ToListAsync());
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync()
    {
        return await DbSet.GroupBy(ua => ua.ZipCode).ToListAsync();
    }

    public async Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync()
    {
        return await DbSet.GroupBy(ua => ua.City).ToListAsync();
    }
}
