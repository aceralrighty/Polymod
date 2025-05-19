using TBD.Models.DTOs;
using TBD.Repository.Base;

namespace TBD.Repository.UserAddress;

public interface IUserAddressRepository:IGenericRepository<Models.Entities.UserAddress>
{
    Task<Models.Entities.UserAddress> UpdateUserAddress(UserAddressRequest userAddress);
}