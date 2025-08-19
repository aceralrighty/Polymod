using PolyMod.StockPredictionModule.Models;
using PolyMod.StockPredictionModule.Models.Stocks;

namespace PolyMod.StockPredictionModule.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath);
    Task<double?> PerformQuickAccuracyCheck(Dictionary<string, List<RawData>> groupedBySymbol);
}
