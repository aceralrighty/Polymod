using StockPredictionService.Models.Stocks;

namespace StockPredictionService.Repository.Interfaces;

public interface IStockPredictionRepository
{
    Task<IEnumerable<StockPrediction>> GetLatestStockPredictionsAsync(Guid id, int count = 50);
    Task<IEnumerable<StockPrediction>> GetStocksByBatchAsync(Guid batchId);
    Task SaveStockPredictionBatchAsync(IEnumerable<StockPrediction> stockPredictions);
    Task<StockPrediction> SaveStockPredictionAsync(StockPrediction stockPrediction);
    Task<IEnumerable<Stock>> GetStockPredictionsBySymbolAsync(string symbol, CancellationToken ct = default);

    Task<IEnumerable<StockPrediction>> GetPredictionsBySymbolAsync(string symbol, CancellationToken ct = default);
}
