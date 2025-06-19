using TBD.StockPredictionModule.Load;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.PipelineOrchestrator.Interface;
using TBD.StockPredictionModule.Repository;

namespace TBD.StockPredictionModule.PipelineOrchestrator;

public class StockPredictionPipeline(
    LoadCsvData csvLoader,
    DataTransformation dataTransformer,
    MlStockPredictionEngine mlEngine,
    IStockPredictionRepository stockPredictionRepository)
    : IStockPredictionPipeline
{
    public async Task<StockPrediction> ExecuteFullPipelineAsync(string csvFilePath, string symbolToPredict)
    {
        try
        {
            Console.WriteLine("Step 1: Loading CSV to memory...");
            var rawData = await csvLoader.LoadRawDataAsync(csvFilePath);
            Console.WriteLine($"Loaded {rawData.Count} records into memory");

            Console.WriteLine("Step 2: Transforming raw data...");
            var stocks = dataTransformer.TransformRawDataToStocks(rawData);

            Console.WriteLine("Step 3: Training model from in-memory data...");
            await mlEngine.TrainModelAsync(rawData); // accepts rawData directly

            Console.WriteLine($"Step 4: Generating prediction for {symbolToPredict}...");
            await mlEngine.TrainModelAsync(rawData);
            var prediction = await mlEngine.GeneratePredictAsync(rawData, symbolToPredict);

            Console.WriteLine($"Step 5: Saving prediction to database...");
            await stockPredictionRepository.SaveStockPredictionBatchAsync([prediction]);


            Console.WriteLine("Pipeline completed successfully!");
            return prediction;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing pipeline: {ex.Message}");
            throw;
        }
    }

    public Task<StockPrediction> GeneratePredictionOnly(string symbolToPredict)
    {
        throw new NotImplementedException();
    }
}
