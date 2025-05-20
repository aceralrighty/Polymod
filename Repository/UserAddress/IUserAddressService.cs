using TBD.Repository.Base;

namespace TBD.Repository.UserAddress;

public interface IUserAddressService:IGenericRepository<Models.Entities.UserAddress>
{
    Task<List<IGrouping<string?, Models.Entities.UserAddress>>> GroupByUserStateAsync(Models.Entities.UserAddress userAddress);
    
    Task<List<IGrouping<int, Models.Entities.UserAddress>>> GroupByZipCodeAsync(Models.Entities.UserAddress userAddress);
    Task<List<IGrouping<string?, Models.Entities.UserAddress>>> GroupByCityAsync(Models.Entities.UserAddress userAddress);
    Task<Models.Entities.UserAddress> GetByUserAddressAsync(Models.Entities.UserAddress userAddress);
    
}