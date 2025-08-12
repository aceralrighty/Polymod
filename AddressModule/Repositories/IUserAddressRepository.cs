using TBD.AddressModule.Models;

namespace TBD.AddressModule.Repositories;

public interface IUserAddressRepository
{
    // Basic CRUD operations (needed by gRPC service)
    Task<UserAddress?> GetByIdAsync(Guid id);
    Task<UserAddress> CreateAsync(UserAddress userAddress);
    Task<UserAddress> UpdateAsync(UserAddress userAddress);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<UserAddress>> GetByUserIdAsync(Guid userId);

    // Existing custom methods
    Task<UserAddress> GetByUserAddressAsync(UserAddress userAddress);
    Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync();
}
