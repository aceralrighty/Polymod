using AutoMapper;
using TBD.API.DTOs;
using TBD.ServiceModule.Models;

namespace TBD.Shared.Utils;

public class ServiceMapping : Profile
{
    public ServiceMapping()
    {
        CreateMap<Service, ServiceDTO>();
        CreateMap<ServiceDTO, Service>().ReverseMap();
    }
}
