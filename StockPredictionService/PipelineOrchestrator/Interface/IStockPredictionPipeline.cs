using PolyMod.StockPredictionService.Models;
using PolyMod.StockPredictionService.Models.Stocks;

namespace PolyMod.StockPredictionService.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath);
    Task<double?> PerformQuickAccuracyCheck(Dictionary<string, List<RawData>> groupedBySymbol);
}
