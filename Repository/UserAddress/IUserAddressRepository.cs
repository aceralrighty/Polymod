using TBD.Models.DTOs;
using TBD.Repository.Services.Base;

namespace TBD.Repository.Services.UserAddress;

public interface IUserAddressRepository:IGenericRepository<Models.Entities.UserAddress>
{
    Task<Models.Entities.UserAddress> UpdateUserAddress(UserAddressRequest userAddress);
}