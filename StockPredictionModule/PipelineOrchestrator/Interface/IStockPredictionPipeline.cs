using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.PipelineOrchestrator.Interface;

public interface IStockPredictionPipeline
{
    Task<StockPrediction> ExecuteFullPipelineAsync(string csvFilePath, string symbolToPredict);
    Task<StockPrediction> GeneratePredictionOnly(string symbolToPredict);
}
