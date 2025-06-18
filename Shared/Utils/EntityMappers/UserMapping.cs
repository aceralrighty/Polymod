using AutoMapper;
using TBD.API.DTOs;
using TBD.API.DTOs.Users;
using TBD.UserModule.Models;

namespace TBD.Shared.Utils.EntityMappers;

public class UserMapping : Profile
{
    public UserMapping()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}
