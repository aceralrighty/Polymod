using AutoMapper;
using TBD.AddressModule.Models;
using TBD.API.DTOs;

namespace TBD.Shared.Utils;

public class UserAddressMapping : Profile
{
    public UserAddressMapping()
    {
        CreateMap<UserAddressRequest, UserAddress>()
            .ForMember(dest => dest.UserId, opt => opt.Condition(src => src.UserId != Guid.Empty))
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<UserAddress, UserAddressResponse>();
    }
}
