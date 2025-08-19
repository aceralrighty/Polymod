using PolyMod.StockPredictionService.Models;
using PolyMod.StockPredictionService.Models.Stocks;

namespace PolyMod.StockPredictionService.ML.Interface;

public interface IMlStockPredictionEngine
{
    Task<bool> IsModelTrainedAsync();
    Task TrainModelAsync(List<RawData> rawData);
    Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol);
    Task TrainModelStreamingAsync(string csvFilePath);
    Task<StockPrediction> GeneratePredictAsync(Dictionary<string, List<RawData>> groupedData, string symbol);
}
