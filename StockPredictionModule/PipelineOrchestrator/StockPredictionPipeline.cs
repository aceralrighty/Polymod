using TBD.Shared.EntityMappers;
using TBD.StockPredictionModule.Load;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator.Interface;
using TBD.StockPredictionModule.Repository;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.PipelineOrchestrator;

public class StockPredictionPipeline(
    StockEntityMapper entityMapper,
    MlStockPredictionEngine mlEngine,
    IStockPredictionRepository stockPredictionRepository, IStockRepository stockRepository)
    : IStockPredictionPipeline
{
    public async Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath)
    {
        try
        {
            Console.WriteLine("Step 1: Loading CSV to memory...");
            var rawData = await LoadCsvData.LoadRawDataAsync(csvFilePath);
            Console.WriteLine($"Loaded {rawData.Count} records into memory");

            Console.WriteLine("Step 2: Grouping data by symbol...");
            var groupedBySymbol = rawData
                .GroupBy(r => r.Symbol)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => DateTime.Parse(r.Date)).ToList());

            Console.WriteLine($"Found {groupedBySymbol.Count} unique symbols");

            Console.WriteLine("Step 3: Training model with all historical data...");
            await mlEngine.TrainModelAsync(rawData);

            // Simple accuracy check - compare a few recent predictions to actual values
            Console.WriteLine("Step 4: Quick accuracy check...");
            await PerformQuickAccuracyCheck(groupedBySymbol);

            var allPredictions = new List<StockPrediction>();
            var batchId = Guid.NewGuid(); // Single batch ID for all predictions

            Console.WriteLine("Step 5: Generating predictions for each symbol...");
            var symbolCount = 0;
            foreach (var (symbol, symbolRawData) in groupedBySymbol)
            {
                symbolCount++;

                try
                {
                    Console.WriteLine(
                        $"Processing {symbol} ({symbolCount}/{groupedBySymbol.Count}) - {symbolRawData.Count} records");

                    // Generate prediction for this symbol
                    var prediction = await mlEngine.GeneratePredictAsync(rawData, symbol);
                    prediction.BatchId = batchId; // Set a consistent batch ID

                    allPredictions.Add(prediction);
                    Console.WriteLine($"✅ Prediction for {symbol}: ${prediction.Price:F2}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to process {symbol}: {ex.Message}");
                    // Continue with other symbols
                }
            }
            var stonks = entityMapper.TransformRawDataToStocks(rawData);

            Console.WriteLine($"Step 6: Saving {allPredictions.Count} predictions to database...");
            await stockPredictionRepository.SaveStockPredictionBatchAsync(allPredictions);
            await stockRepository.SaveStockAsync(stonks); // Save all stocks (for future reference)

            Console.WriteLine(
                $"Pipeline completed successfully! Generated predictions for {allPredictions.Count} symbols");
            return allPredictions;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing pipeline: {ex.Message}");
            throw;
        }
    }

    // Simple accuracy check using recent historical data
    private async Task PerformQuickAccuracyCheck(Dictionary<string, List<RawData>> groupedBySymbol)
    {
        var testSymbols = groupedBySymbol.Take(5).ToList(); // Test first 5 symbols
        var totalError = 0.0;
        var testCount = 0;

        foreach (var (symbol, historicalData) in testSymbols)
        {
            if (historicalData.Count < 10) continue; // Need enough data

            // Use second-to-last record to predict last record
            var secondLast = historicalData[^2];
            var actual = historicalData[^1];

            try
            {
                var testData = historicalData.Take(historicalData.Count - 1).ToList();
                var prediction = await mlEngine.GeneratePredictAsync(testData, symbol);

                var error = Math.Abs(prediction.Price - actual.Close);
                var percentageError = (error / actual.Close) * 100;

                totalError += percentageError;
                testCount++;

                Console.WriteLine(
                    $"   {symbol}: Predicted ${prediction.Price:F2}, Actual ${actual.Close:F2}, Error: {percentageError:F1}%");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   {symbol}: Could not test accuracy - {ex.Message}");
            }
        }

        if (testCount > 0)
        {
            var avgError = totalError / testCount;
            var accuracyRating = avgError < 5 ? "🟢 GOOD" : avgError < 10 ? "🟡 FAIR" : "🔴 POOR";
            Console.WriteLine($"📊 Average prediction error: {avgError:F1}% - {accuracyRating}");
        }
        else
        {
            Console.WriteLine("⚠️ Could not perform accuracy check");
        }
    }
}
