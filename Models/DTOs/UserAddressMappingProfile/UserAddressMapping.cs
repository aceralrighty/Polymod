using AutoMapper;
using TBD.Models.Entities;

namespace TBD.Models.DTOs.UserAddressMappingProfile;

public class UserAddressMapping : Profile
{
    public UserAddressMapping()
    {
        CreateMap<UserAddressRequest, UserAddress>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));
    }
}