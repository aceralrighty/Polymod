using AutoMapper;
using TBD.API.DTOs;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;

namespace TBD.ServiceModule.Services;

internal class ServicesService(IServiceRepository repository, IMapper mapper) : IServicesService
{
    public async Task<IEnumerable<ServiceDto>> GetAllServicesAsync()
    {
        var service = await repository.GetAllServicesAsync();
        return mapper.Map<IEnumerable<ServiceDto>>(service);
    }

    public async Task<ServiceDto> GetByIdAsync(Guid id)
    {
        var service = await repository.GetByIdAsync(id);
        return mapper.Map<ServiceDto>(service);
    }

    public async Task<ServiceDto> GetByTitleAsync(string title)
    {
        var service = await repository.GetByTitleAsync(title);
        return mapper.Map<ServiceDto>(service);
    }

    public async Task CreateAsync(ServiceDto serviceDto)
    {
        var s = mapper.Map<Service>(serviceDto);
        await repository.AddAsync(s);
    }

    public async Task UpdateAsync(ServiceDto serviceDto)
    {
        var s = mapper.Map<Service>(serviceDto);
        await repository.UpdateAsync(s);
    }

    public async Task DeleteAsync(Guid id)
    {
        var s = await repository.GetByIdAsync(id);
        await repository.DeleteAsync(s.Id);
    }
}
