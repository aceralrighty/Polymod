using Microsoft.EntityFrameworkCore;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.Repository;

public class StockPredictionRepository(StockDbContext context)
    : GenericRepository<StockPrediction>(context), IStockPredictionRepository
{
    public async Task<IEnumerable<StockPrediction>> GetLatestStockPredictionsAsync(Guid id, int count = 50)
    {
        return await context.StockPredictions.Where(sp => sp.Id == id).OrderByDescending(sp => sp.CreatedAt).Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<StockPrediction>> GetStocksByBatchAsync(Guid batchId)
    {
        return await context.StockPredictions.Where(sp => sp.BatchId == batchId).OrderBy(sp => sp.CreatedAt)
            .ToListAsync();
    }
    public async Task<StockPrediction> SaveStockPredictionAsync(StockPrediction stockPrediction)
    {
        ArgumentNullException.ThrowIfNull(stockPrediction);

        try
        {
            if (stockPrediction.CreatedAt == default)
                stockPrediction.CreatedAt = DateTime.UtcNow;
            if (stockPrediction.UpdatedAt == default)
                stockPrediction.UpdatedAt = DateTime.UtcNow;

            await context.StockPredictions.AddAsync(stockPrediction);
            Console.WriteLine($"Added stock prediction record for ${stockPrediction.PredictedPrice:F2}");

            var savedCount = await context.SaveChangesAsync();
            Console.WriteLine($"Successfully saved {savedCount} stock prediction record");

            var exists = await context.StockPredictions
                .AnyAsync(sp => sp.Id == stockPrediction.Id);
            Console.WriteLine($"Verification check - Stock prediction exists: {exists}");

            return stockPrediction;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving stock prediction record: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }

            throw;
        }
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

    public async Task<IEnumerable<StockPrediction>> GetPredictionsBySymbolAsync(string symbol)
    {
        // Join with Stock data to filter by symbol
        return await context.StockPredictions
            .Join(context.Stocks,
                sp => sp.BatchId,
                s => s.Id, // You may need to adjust this join condition
                (sp, s) => new { Prediction = sp, Stock = s })
            .Where(joined => joined.Stock.Symbol == symbol)
            .Select(joined => joined.Prediction)
            .OrderByDescending(sp => sp.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Stock>> GetStockPredictionsBySymbolAsync(string symbol)
    {
        return await context.Stocks.GroupBy(s => s.Symbol == symbol).Select(g => g.First()).ToListAsync();
    }
}
