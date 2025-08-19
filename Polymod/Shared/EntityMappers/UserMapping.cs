using AutoMapper;
using PolyMod.API.DTOs.Users;
using PolyMod.UserModule.Models;

namespace PolyMod.Shared.EntityMappers;

public class UserMapping : Profile
{
    public UserMapping()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}
