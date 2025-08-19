using AutoMapper;
using TBD.API.DTOs.AuthDTO;
using TBD.AuthModule.Models;

namespace TBD.Shared.EntityMappers;

public class AuthUserMapping : Profile
{
    public AuthUserMapping()
    {
        CreateMap<AuthUser, RegisterRequest>();
        CreateMap<RegisterRequest, AuthUser>().ReverseMap();
    }
}
