using TBD.AddressModule.Models;

namespace TBD.AddressModule.Services;

public interface IUserAddressService
{
    Task<IEnumerable<UserAddress>> GetAllAsync();
    Task<UserAddress?> GetByIdAsync(Guid id);
    Task AddAsync(UserAddress entity);
    Task UpdateAsync(UserAddress entity);
    Task RemoveAsync(UserAddress entity);
    Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto);
}