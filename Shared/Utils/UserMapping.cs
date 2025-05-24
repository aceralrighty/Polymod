using AutoMapper;
using TBD.API.DTOs;
using TBD.UserModule.Models;

namespace TBD.Shared.Utils;

public class UserMapping:Profile
{
    public UserMapping()
    {
        CreateMap<User, UserDto>().ReverseMap();
    }
}