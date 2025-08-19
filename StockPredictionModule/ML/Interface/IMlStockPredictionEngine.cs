using PolyMod.StockPredictionModule.Models;
using PolyMod.StockPredictionModule.Models.Stocks;

namespace PolyMod.StockPredictionModule.ML.Interface;

public interface IMlStockPredictionEngine
{
    Task<bool> IsModelTrainedAsync();
    Task TrainModelAsync(List<RawData> rawData);
    Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol);
    Task TrainModelStreamingAsync(string csvFilePath);
    Task<StockPrediction> GeneratePredictAsync(Dictionary<string, List<RawData>> groupedData, string symbol);
}
