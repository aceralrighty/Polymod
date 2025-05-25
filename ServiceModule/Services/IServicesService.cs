using TBD.API.DTOs;

namespace TBD.ServiceModule.Services;

internal interface IServicesService
{
    Task<IEnumerable<ServiceDTO>> GetAllServicesAsync();
    Task<ServiceDTO> GetByIdAsync(Guid id);
    Task<ServiceDTO> GetByTitleAsync(string title);
    
    Task CreateAsync(ServiceDTO service);
    Task UpdateAsync(ServiceDTO service);
    Task DeleteAsync(Guid id);
}