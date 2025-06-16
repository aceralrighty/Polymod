using AutoMapper;
using TBD.AddressModule.Models;
using TBD.API.DTOs;
using TBD.API.DTOs.UserDTO;

namespace TBD.Shared.Utils.EntityMappers;

public class UserAddressMapping : Profile
{
    public UserAddressMapping()
    {
        CreateMap<UserAddressRequest, UserAddress>()
            .ForMember(dest => dest.UserId, opt => opt.Condition(src => src.UserId != Guid.Empty))
            .ForAllMembers(opt => opt.Condition((_, _, srcMember) => srcMember != null));
        CreateMap<UserAddress, UserAddressResponse>();
    }
}
