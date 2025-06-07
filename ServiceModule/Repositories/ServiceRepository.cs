using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;
using TBD.Shared.Repositories;

namespace TBD.ServiceModule.Repositories;

internal class ServiceRepository(ServiceDbContext context)
    : GenericRepository<Service>(context), IServiceRepository
{
    public override async Task<Service?> GetByIdAsync(Guid id)
    {
        return await _dbSet.FindAsync(id);
    }

    public async Task<IEnumerable<Service>> GetAllServicesAsync()
    {
        return await _dbSet.ToListAsync();
    }

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

    public async Task DeleteAsync(Guid id)
    {
        var service = await _dbSet.FindAsync(id);
        _dbSet.Remove(service ?? throw new InvalidOperationException("Service not found"));
        await _context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Service> services)
    {
        await _dbSet.AddRangeAsync(services);
    }
}
