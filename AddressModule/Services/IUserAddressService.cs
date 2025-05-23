using TBD.AddressModule.Models;
using TBD.Shared.DTOs;

namespace TBD.AddressModule.Services;

internal interface IUserAddressService
{
    Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto);
}