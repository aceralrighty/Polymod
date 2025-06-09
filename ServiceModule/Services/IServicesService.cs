using TBD.API.DTOs;

namespace TBD.ServiceModule.Services;

internal interface IServicesService
{
    Task<IEnumerable<ServiceDto>> GetAllServicesAsync();
    Task<ServiceDto> GetByIdAsync(Guid id);
    Task<ServiceDto> GetByTitleAsync(string title);

    Task CreateAsync(ServiceDto service);
    Task UpdateAsync(ServiceDto service);
    Task DeleteAsync(Guid id);
}
