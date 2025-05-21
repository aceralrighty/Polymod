using TBD.AddressService.Models;
using TBD.Models.DTOs;
using TBD.Shared.DTOs;

namespace TBD.AddressService.Services;

internal interface IUserAddressService
{
    Task<UserAddress> UpdateUserAddress(UserAddressRequest userAddressDto);
}