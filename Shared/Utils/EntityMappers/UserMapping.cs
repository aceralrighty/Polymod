using AutoMapper;
using TBD.API.DTOs;
using TBD.API.DTOs.UserDTO;
using TBD.UserModule.Models;

namespace TBD.Shared.Utils.EntityMappers;

public class UserMapping : Profile
{
    public UserMapping()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}
