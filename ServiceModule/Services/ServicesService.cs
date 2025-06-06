using AutoMapper;
using TBD.API.DTOs;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;

namespace TBD.ServiceModule.Services;

internal class ServicesService(IServiceRepository repository, IMapper mapper) : IServicesService
{
    public async Task<IEnumerable<ServiceDTO>> GetAllServicesAsync()
    {
        var service = await repository.GetAllServicesAsync();
        return mapper.Map<IEnumerable<ServiceDTO>>(service);
    }

    public async Task<ServiceDTO> GetByIdAsync(Guid id)
    {
        var service = await repository.GetByIdAsync(id);
        return mapper.Map<ServiceDTO>(service);
    }

    public async Task<ServiceDTO> GetByTitleAsync(string title)
    {
        var service = await repository.GetByTitleAsync(title);
        return mapper.Map<ServiceDTO>(service);
    }

    public async Task CreateAsync(ServiceDTO serviceDto)
    {
        var s = mapper.Map<Service>(serviceDto);
        await repository.AddAsync(s);
    }

    public async Task UpdateAsync(ServiceDTO serviceDto)
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
