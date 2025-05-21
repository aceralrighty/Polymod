using TBD.AddressService.Models;

namespace TBD.AddressService.Repositories;

public interface IUserAddressRepository
{
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    Task<List<IGrouping<string, UserAddress>>> GroupByUserStateAsync();
    Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync();
    Task<List<IGrouping<string, UserAddress>>> GroupByCityAsync();
}