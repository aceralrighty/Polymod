using AutoMapper;
using PolyMod.API.DTOs.AuthDTO;
using PolyMod.AuthModule.Models;

namespace PolyMod.Shared.EntityMappers;

public class AuthUserMapping : Profile
{
    public AuthUserMapping()
    {
        CreateMap<AuthUser, RegisterRequest>();
        CreateMap<RegisterRequest, AuthUser>().ReverseMap();
    }
}
