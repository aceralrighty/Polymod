using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;
using TBD.TradingModule.Infrastructure.MarketData;

namespace TBD.TradingModule.Orchestration;

public class TrainingOrchestrator(
    RawCsvData dataFetcher,
    ITradingRepository repository,
    ILogger<TrainingOrchestrator> logger)
{
    private readonly ITradingRepository _repository = repository;

    /// <summary>
    /// Load training data from a CSV file or directory of files.
    /// </summary>
    public async Task<Dictionary<string, List<RawMarketData>>> LoadTrainingDataFromCsvAsync(
        string csvPath,
        List<string>? symbols = null,
        int yearsBack = 5)
    {
        var startDate = DateTime.UtcNow.AddYears(-yearsBack);
        var endDate = DateTime.UtcNow;

        Dictionary<string, List<RawMarketData>> marketData;

        if (Directory.Exists(csvPath))
        {
            // Load from a directory with one CSV per symbol
            var allData = await dataFetcher.LoadFromDirectoryAsync(csvPath);
            marketData = allData.ToDictionary(kvp => kvp.Key,
                kvp => kvp.Value.ValidateAndClean(logger));
        }
        else if (File.Exists(csvPath))
        {
            // Load from a single large CSV containing multiple symbols
            marketData = await dataFetcher.LoadBatchFromSingleCsvAsync(csvPath, startDate, endDate);
            marketData = marketData
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ValidateAndClean(logger));
        }
        else
        {
            throw new FileNotFoundException($"CSV path not found: {csvPath}");
        }

        // Optional: filter symbols
        if (symbols is { Count: > 0 })
        {
            marketData = marketData
                .Where(kvp => symbols.Contains(kvp.Key))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        logger.LogInformation("Loaded local market data for {Count} symbols", marketData.Count);
        return marketData;
    }

    /// <summary>
    /// This can be called to train your pipeline on preloaded CSV-based data.
    /// </summary>
    public async Task TrainFromCsvAsync(string csvPath, List<string>? symbols = null)
    {
        var marketData = await LoadTrainingDataFromCsvAsync(csvPath, symbols);

        if (marketData.Count == 0)
        {
            logger.LogWarning("No valid training data loaded.");
            return;
        }

        // You would now pass `marketData` into your preprocessing and training pipeline
        // Example: var featureVectors = FeatureEngineer.Transform(marketData);
        // Example: ModelTrainer.Train(featureVectors);

        logger.LogInformation("Ready to train model with {Count} symbol(s)", marketData.Count);
    }
}
