using TBD.StockPredictionModule.Models.Stocks;

namespace TBD.StockPredictionModule.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath);
}
