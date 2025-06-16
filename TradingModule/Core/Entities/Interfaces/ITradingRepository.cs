using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.DataAccess.Interfaces;

public interface ITradingRepository
{
// Raw market data operations
    Task SaveMarketDataAsync(List<RawMarketData> marketData);
    Task<List<RawMarketData>> GetMarketDataAsync(string symbol, DateTime startDate, DateTime endDate);
    Task<DateTime?> GetLatestDataDateAsync(string symbol);

    // Feature vector operations
    Task SaveFeatureVectorsAsync(List<StockFeatureVector> features);
    Task<List<StockFeatureVector>> GetFeatureVectorsAsync(string symbol, DateTime startDate, DateTime endDate);

    // Prediction operations
    Task SavePredictionAsync(PredictionResult prediction);
    Task<List<PredictionResult>> GetPredictionsAsync(DateTime predictionDate);
    Task UpdateActualResultsAsync(string symbol, DateTime targetDate, float actualReturn, float actualVolatility);



}
