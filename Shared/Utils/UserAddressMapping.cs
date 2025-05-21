using AutoMapper;
using TBD.AddressService.Models;
using TBD.Shared.DTOs;

namespace TBD.Shared.Utils;

public class UserAddressMapping : Profile
{
    public UserAddressMapping()
    {
        CreateMap<UserAddressRequest, UserAddress>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}