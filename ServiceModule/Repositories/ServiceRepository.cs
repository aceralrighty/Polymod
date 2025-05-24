using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;

namespace TBD.ServiceModule.Repositories;

internal class ServiceRepository(ServiceDbContext context)
    : GenericServiceRepository<Service>(context), IServiceRepository
{
    public async Task<Service> GetByTitleAsync(string title)
    {
        return await _dbSet.FirstOrDefaultAsync(s => s.Title == title) ?? throw new Exception();
    }

    public async Task<List<Service>> SortByHighestMinutesAsync()
    {
        return await _dbSet.OrderByDescending(s => s.DurationInMinutes).ToListAsync();
    }
    
    public async Task<List<Service>> SortByHighestPriceAsync()
    {
        return await _dbSet.OrderByDescending(s => s.Price).ToListAsync();
    }
    
}