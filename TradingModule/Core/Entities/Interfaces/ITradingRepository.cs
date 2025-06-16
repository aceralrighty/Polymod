namespace TBD.TradingModule.Core.Entities.Interfaces;

public interface ITradingRepository
{
    Task SaveMarketDataAsync(List<RawMarketData> marketData);
    Task<List<RawMarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<DateTime?> GetLatestDataDateAsync(string symbol);
    Task<bool> HasDataForSymbolAsync(string symbol, DateTime date);

    // Feature vector operations
    Task SaveFeatureVectorsAsync(List<StockFeatureVector> features);
    Task<List<StockFeatureVector>> GetFeatureVectorsAsync(string symbol, DateTime startDate, DateTime endDate);

    // Prediction operations
    Task SavePredictionAsync(PredictionResult prediction);
    Task SavePredictionsAsync(List<PredictionResult> predictions); // Bulk save
    Task<List<PredictionResult>> GetPredictionsAsync(DateTime predictionDate);
    Task<List<PredictionResult>> GetPredictionsBySymbolAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<PredictionResult?> GetLatestPredictionAsync(string symbol);
    Task UpdateActualResultsAsync(string symbol, DateTime targetDate, float actualReturn, float actualVolatility);

    // Performance and analytics
    Task<List<PredictionResult>> GetPredictionsForBacktestingAsync(DateTime startDate, DateTime endDate);
    Task<Dictionary<string, int>> GetDataCountBySymbolAsync(DateTime startDate, DateTime endDate);

    // Cleanup operations
    Task CleanupOldDataAsync(DateTime cutoffDate);
    Task CleanupOldPredictionsAsync(DateTime cutoffDate);
    Task<int> GetApiRequestCountAsync(string provider, DateTime since);
    Task SaveApiRequestLogAsync(ApiRequestLog log);
}
