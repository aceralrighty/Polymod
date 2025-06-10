using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;
using TBD.Shared.Repositories;

namespace TBD.ServiceModule.Repositories;

internal class ServiceRepository(ServiceDbContext context)
    : GenericRepository<Service>(context), IServiceRepository
{
    public override async Task<Service> GetByIdAsync(Guid id)
    {
        return await DbSet.FindAsync(id) ?? throw new NullReferenceException();
    }

    public async Task<IEnumerable<Service>> GetAllServicesAsync()
    {
        return await DbSet.ToListAsync();
    }

    public async Task<Service> GetByTitleAsync(string title)
    {
        return await DbSet.FirstOrDefaultAsync(s => s.Title == title) ?? throw new Exception();
    }

    public async Task<List<Service>> SortByHighestMinutesAsync()
    {
        return await DbSet.OrderByDescending(s => s.DurationInMinutes).ToListAsync();
    }

    public async Task<List<Service>> SortByHighestPriceAsync()
    {
        return await DbSet.OrderByDescending(s => s.Price).ToListAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var service = await DbSet.FindAsync(id);
        DbSet.Remove(service ?? throw new InvalidOperationException("Service not found"));
        await Context.SaveChangesAsync();
    }

    public async Task AddRangeAsync(IEnumerable<Service> services)
    {
        await DbSet.AddRangeAsync(services);
    }
}
