using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Repository;

public interface IStockPredictionRepository
{
    Task<IEnumerable<StockPrediction>> GetLatestStockPredictionsAsync(Guid id, int count = 50);
    Task<IEnumerable<StockPrediction>> GetStocksByBatchAsync(Guid batchId);
    Task SaveStockPredictionBatchAsync(IEnumerable<StockPrediction> stockPredictions);

}
