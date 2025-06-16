using TBD.AddressModule.Models;

namespace TBD.AddressModule.Repositories;

public interface IUserAddressRepository
{
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync();
}
