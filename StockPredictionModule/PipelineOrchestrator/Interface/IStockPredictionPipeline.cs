using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath);
}
