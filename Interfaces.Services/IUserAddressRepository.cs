using TBD.Models;
using TBD.Repository;

namespace TBD.Interfaces.Services;

public interface IUserAddressRepository:IGenericRepository<UserAddress>
{
    Task<List<IGrouping<string, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress);
    
    Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress);
    Task<List<IGrouping<string, UserAddress>>> GroupByCityAsync(UserAddress userAddress);
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    
}