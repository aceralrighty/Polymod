using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.ML.Interface;

public interface IMlStockPredictionEngine
{

    Task<bool> IsModelTrainedAsync();
    Task TrainModelAsync(List<RawData> rawData);
    Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol);
    List<RawData> CleanTrainingData(List<RawData> rawData);
}
