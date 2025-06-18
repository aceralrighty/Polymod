using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Repository;

public class StockPredictionRepository(StockDbContext context)
    : GenericRepository<StockPrediction>(context), IStockPredictionRepository
{
    public async Task<IEnumerable<StockPrediction>> GetLatestStockPredictionsAsync(Guid id, int count = 50)
    {
        return await context.StockPredictions.Where(sp => sp.Id == id).OrderByDescending(sp => sp.CreatedAt).Take(count)
            .Include(sp => sp.Price).ToListAsync();
    }

    public async Task<IEnumerable<StockPrediction>> GetStocksByBatchAsync(Guid batchId)
    {
        return await context.StockPredictions.Where(sp => sp.BatchId == batchId).OrderBy(sp => sp.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveStockPredictionBatchAsync(IEnumerable<StockPrediction> stockPredictions)
    {
        var stockPredictionList = stockPredictions.ToList();
        if (stockPredictionList.Count == 0)
        {
            Console.WriteLine("Stock Prediction list is empty");
            return;
        }

        try
        {
            foreach (var stockPrediction in stockPredictionList)
            {
                if (stockPrediction.CreatedAt == default)
                    stockPrediction.CreatedAt = DateTime.UtcNow;
                if (stockPrediction.UpdatedAt == default)
                    stockPrediction.UpdatedAt = DateTime.UtcNow;
            }

            await context.StockPredictions.AddRangeAsync(stockPredictionList);
            Console.WriteLine($"Added {stockPredictionList.Count} stock prediction records");

            var savedCount = await context.SaveChangesAsync();
            Console.WriteLine($"Successfully saved {savedCount} stock prediction records");

            var firstStock = stockPredictionList.First();
            var exists = await context.StockPredictions
                .AnyAsync(sp => sp.Id == firstStock.Id);
            Console.WriteLine($"Verification check - First stock prediction exists: {exists}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving stock prediction records: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }

            throw;
        }
    }
}
