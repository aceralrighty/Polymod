using AutoMapper;
using PolyMod.API.DTOs;
using PolyMod.ServiceModule.Models;

namespace PolyMod.Shared.EntityMappers;

public class ServiceMapping : Profile
{
    public ServiceMapping()
    {
        CreateMap<Service, ServiceDto>();
        CreateMap<ServiceDto, Service>().ReverseMap();
    }
}
