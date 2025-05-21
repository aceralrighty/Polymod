using TBD.AddressService.Models;

namespace TBD.AddressService.Repositories;

public interface IUserAddressRepository
{
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress);
    Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress);
    Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync(UserAddress userAddress);
}