using System.Linq.Expressions;
using PolyMod.AddressModule.Models;
using PolyMod.API.DTOs.Users;

namespace PolyMod.AddressModule.Services;

public interface IUserAddressService
{
    // High-level service operations
    Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto);
    Task<IEnumerable<UserAddress>> GetAllAsync(Guid userId);
    Task<IEnumerable<UserAddress>> FindAsync(Expression<Func<UserAddress, bool>> expression);
    Task AddAsync(UserAddress entity);
    Task AddRangeAsync(IEnumerable<UserAddress> entities);
    Task UpdateAsync(UserAddress entity);
    Task RemoveAsync(UserAddress entity);

    // Grouping operations
    Task<List<IGrouping<string?, UserAddress>>> GroupByUserStateAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByZipCodeAsync();
    Task<List<IGrouping<string?, UserAddress>>> GroupByCityAsync();
}
