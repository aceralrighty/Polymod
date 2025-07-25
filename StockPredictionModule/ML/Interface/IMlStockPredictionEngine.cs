using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.StockPredictionModule.ML.Interface;

public interface IMlStockPredictionEngine
{
    Task<bool> IsModelTrainedAsync();
    Task TrainModelAsync(List<RawData> rawData);
    Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol);
    List<RawData> CleanTrainingData(List<RawData> rawData);

    // New streaming methods
    Task TrainModelStreamingAsync(string csvFilePath);
    Task<StockPrediction> GeneratePredictAsync(Dictionary<string, List<RawData>> groupedData, string symbol);
}
