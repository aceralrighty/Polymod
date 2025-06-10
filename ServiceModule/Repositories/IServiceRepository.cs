using TBD.ServiceModule.Models;

namespace TBD.ServiceModule.Repositories;

internal interface IServiceRepository
{
    Task<IEnumerable<Service>> GetAllServicesAsync();
    Task<Service> GetByTitleAsync(string title);
    Task<List<Service>> SortByHighestMinutesAsync();
    Task<List<Service>> SortByHighestPriceAsync();
    Task<Service> GetByIdAsync(Guid id);
    Task AddAsync(Service service);
    Task UpdateAsync(Service service);
    Task DeleteAsync(Guid id);
    Task AddRangeAsync(IEnumerable<Service> services);
}
