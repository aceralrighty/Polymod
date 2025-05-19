using TBD.Models.Entities;
using TBD.Repository.Base;

namespace TBD.Interfaces.Services;

public interface IUserAddressService:IGenericRepository<UserAddress>
{
    Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync(UserAddress userAddress);
    
    Task<List<IGrouping<int, UserAddress>>> GroupByZipCodeAsync(UserAddress userAddress);
    Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync(UserAddress userAddress);
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    
}