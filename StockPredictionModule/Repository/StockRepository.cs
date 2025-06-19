using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.Repository;

public class StockRepository(StockDbContext context) : GenericRepository<RawData>(context), IStockRepository
{
    public async Task<RawData?> GetByTableIdAsync(Guid id)
    {
        return await DbSet.FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task SaveStockAsync(List<Stock> stock)
    {
        await context.Stocks.AddRangeAsync(stock);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<RawData>> GetBySymbolAsync(string symbol)
    {
        return await DbSet.Where(f => f.Symbol == symbol).OrderByDescending(f => f.Date).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByHighestVolumeAsync(float volume)
    {
        return await DbSet.Where(f => f.Volume > volume).OrderByDescending(f => f.Volume).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByLowestCloseAsync(float close)
    {
        return await DbSet.Where(f => f.Close < close).OrderBy(f => f.Close).ToListAsync();
    }

    public async Task<IEnumerable<RawData>> GetByLatestDateAsync(string date)
    {
        return await DbSet.Where(f => f.Date == date).OrderByDescending(f => f.Date).ToListAsync();
    }
}
