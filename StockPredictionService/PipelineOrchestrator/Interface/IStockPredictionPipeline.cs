using StockPredictionService.Models;
using StockPredictionService.Models.Stocks;

namespace StockPredictionService.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath);
    Task<double?> PerformQuickAccuracyCheck(Dictionary<string, List<RawData>> groupedBySymbol);
}
