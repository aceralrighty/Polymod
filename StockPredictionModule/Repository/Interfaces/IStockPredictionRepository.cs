using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.StockPredictionModule.Repository.Interfaces;

public interface IStockPredictionRepository
{
    Task<IEnumerable<StockPrediction>> GetLatestStockPredictionsAsync(Guid id, int count = 50);
    Task<IEnumerable<StockPrediction>> GetStocksByBatchAsync(Guid batchId);
    Task SaveStockPredictionBatchAsync(IEnumerable<StockPrediction> stockPredictions);
    Task<StockPrediction> SaveStockPredictionAsync(StockPrediction stockPrediction);
    Task<IEnumerable<Stock>> GetStockPredictionsBySymbolAsync(string symbol);

    Task<IEnumerable<StockPrediction>> GetPredictionsBySymbolAsync(string symbol);
}
