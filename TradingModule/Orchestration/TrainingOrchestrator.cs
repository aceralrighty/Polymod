using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Core.Entities.Interfaces;
using TBD.TradingModule.Infrastructure.MarketData;
using TBD.TradingModule.ML;
using TBD.TradingModule.Orchestration.Supporting;
using TBD.TradingModule.Preprocessing;

namespace TBD.TradingModule.Orchestration;

public class TrainingOrchestrator(
    MarketDataFetcher marketDataFetcher,
    FeatureEngineeringService featureService,
    StockPredictionEngine predictionEngine,
    ITradingRepository repository, // Using repository instead of DbContext
    ILogger<TrainingOrchestrator> logger)
{
    /// <summary>
    /// Complete pipeline: Fetch data -> Generate features -> Train -> Predict
    /// </summary>
    public async Task<PipelineResult> RunFullPipelineAsync(List<string> symbols, int historicalDays = 365)
    {
        var result = new PipelineResult();
        var startDate = DateTime.Today.AddDays(-historicalDays);
        var endDate = DateTime.Today;

        try
        {
            logger.LogInformation("Starting trading pipeline for {SymbolCount} symbols", symbols.Count);

            // Step 1: Check existing data and determine what needs to be fetched
            var dataToFetch = await DetermineDataToFetchAsync(symbols, startDate, endDate);

            if (dataToFetch.Any())
            {
                logger.LogInformation("Fetching market data for {SymbolCount} symbols from Alpha Vantage...",
                    dataToFetch.Count);
                var batchData = await marketDataFetcher.GetBatchHistoricalDataAsync(dataToFetch, startDate, endDate);

                // Step 2: Store new raw data using repository
                foreach (var kvp in batchData.Where(x => x.Value.Any()))
                {
                    await repository.SaveMarketDataAsync(kvp.Value);
                }

                result.DataPointsFetched = batchData.Values.SelectMany(x => x).Count();
                logger.LogInformation("Fetched and saved {DataPoints} total data points", result.DataPointsFetched);
            }

            // Step 3: Retrieve all required data from repository (both existing and newly fetched)
            var allMarketData = new List<RawMarketData>();
            foreach (var symbol in symbols)
            {
                var symbolData = await repository.GetMarketDataAsync(symbol, startDate, endDate);
                allMarketData.AddRange(symbolData);
            }

            if (!allMarketData.Any())
            {
                throw new InvalidOperationException("No market data available for processing");
            }

            // Step 4: Generate features by symbol
            var allFeatureSets = new List<FeatureEngineeringService.FeatureSet>();
            var allFeatureVectors = new List<StockFeatureVector>();

            foreach (var symbol in symbols)
            {
                var symbolData = allMarketData.Where(r => r.Symbol == symbol).OrderBy(r => r.Date).ToList();

                if (symbolData.Count < 60) // Your feature service needs minimum 50 + buffer
                {
                    logger.LogWarning("Insufficient data for symbol {Symbol}: {Count} records", symbol,
                        symbolData.Count);
                    continue;
                }

                var featureSets = featureService.GenerateFeatureSets(symbolData);
                allFeatureSets.AddRange(featureSets);

                // Extract feature vectors for repository storage
                allFeatureVectors.AddRange(featureSets.Select(fs => fs.Vector));

                logger.LogInformation("Generated {FeatureCount} features for {Symbol}", featureSets.Count, symbol);
            }

            result.FeaturesGenerated = allFeatureSets.Count;

            if (!allFeatureSets.Any())
            {
                throw new InvalidOperationException("No features could be generated from the market data");
            }

            // Step 5: Save feature vectors using repository
            await repository.SaveFeatureVectorsAsync(allFeatureVectors);

            // Step 6: Train model and generate predictions
            logger.LogInformation("Training model and generating predictions...");
            var predictions = await predictionEngine.TrainAndPredictFromFeatureSetsAsync(allFeatureSets);

            // Step 7: Save predictions using repository
            foreach (var prediction in predictions)
            {
                await repository.SavePredictionAsync(prediction);
            }

            result.PredictionsGenerated = predictions.Count;
            result.Predictions = predictions.Select(p => new PredictionSummary
            {
                Symbol = p.Symbol,
                PredictedReturn = p.PredictedReturn,
                ConfidenceScore = p.ConfidenceScore,
                RiskAdjustedScore = p.RiskAdjustedScore,
                PredictionDate = p.PredictionDate,
                TargetDate = p.TargetDate
            }).ToList();

            result.Success = true;
            logger.LogInformation("Pipeline completed successfully. Generated {PredictionCount} predictions",
                predictions.Count);

            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Pipeline failed: {Message}", ex.Message);
            result.Success = false;
            result.ErrorMessage = ex.Message;
            return result;
        }
    }

    /// <summary>
    /// Quick predictions for symbols (uses existing data when possible)
    /// </summary>
    public async Task<List<PredictionSummary>> GenerateQuickPredictionsAsync(List<string> symbols)
    {
        var results = new List<PredictionSummary>();
        var startDate = DateTime.Today.AddDays(-90);
        var endDate = DateTime.Today;

        foreach (var symbol in symbols)
        {
            try
            {
                // First, try to get existing data from repository
                var existingData = await repository.GetMarketDataAsync(symbol, startDate, endDate);

                List<RawMarketData> recentData;

                if (existingData.Count >= 60)
                {
                    // Use existing data
                    recentData = existingData;
                    logger.LogInformation("Using existing data for {Symbol}: {Count} records", symbol,
                        existingData.Count);
                }
                else
                {
                    // Check remaining API requests
                    var remainingRequests = await marketDataFetcher.GetRemainingRequestsAsync();
                    if (remainingRequests <= 0)
                    {
                        logger.LogWarning("API rate limit reached. Skipping {Symbol}", symbol);
                        continue;
                    }

                    // Fetch new data
                    recentData = await marketDataFetcher.GetHistoricalDataAsync(symbol, startDate, endDate);

                    if (recentData.Any())
                    {
                        await repository.SaveMarketDataAsync(recentData);
                    }
                }

                if (recentData.Count < 60)
                {
                    logger.LogWarning("Insufficient data for prediction: {Symbol} ({Count} records)", symbol,
                        recentData.Count);
                    continue;
                }

                // Generate features
                var featureSets = featureService.GenerateFeatureSets(recentData);

                if (!featureSets.Any())
                {
                    logger.LogWarning("No features generated for: {Symbol}", symbol);
                    continue;
                }

                // Get the most recent feature set for prediction
                var latestFeatureSet = featureSets.OrderByDescending(f => f.Vector.Date).First();

                // Generate prediction
                var predictions =
                    await predictionEngine.TrainAndPredictFromFeatureSetsAsync([latestFeatureSet]);

                if (predictions.Count == 0)
                {
                    continue;
                }

                var prediction = predictions.First();
                await repository.SavePredictionAsync(prediction);

                results.Add(new PredictionSummary
                {
                    Symbol = symbol,
                    PredictedReturn = prediction.PredictedReturn,
                    ConfidenceScore = prediction.ConfidenceScore,
                    RiskAdjustedScore = prediction.RiskAdjustedScore,
                    PredictionDate = prediction.PredictionDate,
                    TargetDate = prediction.TargetDate
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to generate prediction for {Symbol}: {Message}", symbol, ex.Message);
            }
        }

        return results;
    }

    /// <summary>
    /// Determines which symbols need data fetched based on existing repository data
    /// </summary>
    private async Task<List<string>> DetermineDataToFetchAsync(List<string> symbols, DateTime startDate,
        DateTime endDate)
    {
        var symbolsToFetch = new List<string>();

        foreach (var symbol in symbols)
        {
            var latestDate = await repository.GetLatestDataDateAsync(symbol);

            // If no data exists or data is stale, add to fetch list
            if (latestDate == null || latestDate < endDate.AddDays(-1))
            {
                symbolsToFetch.Add(symbol);
            }
        }

        return symbolsToFetch;
    }

    /// <summary>
    /// Gets latest predictions for symbols from repository
    /// </summary>
    public async Task<List<PredictionSummary>> GetLatestPredictionsAsync(List<string> symbols)
    {
        var today = DateTime.Today;
        var predictions = await repository.GetPredictionsAsync(today);

        return predictions
            .Where(p => symbols.Contains(p.Symbol))
            .Select(p => new PredictionSummary
            {
                Symbol = p.Symbol,
                PredictedReturn = p.PredictedReturn,
                ConfidenceScore = p.ConfidenceScore,
                RiskAdjustedScore = p.RiskAdjustedScore,
                PredictionDate = p.PredictionDate,
                TargetDate = p.TargetDate
            })
            .ToList();
    }

    public async Task<ApiUsageStatus> GetApiUsageStatusAsync()
    {
        var remaining = await marketDataFetcher.GetRemainingRequestsAsync();
        return new ApiUsageStatus
        {
            RemainingRequests = remaining,
            CanMakeRequests = remaining > 0,
            ResetTime = DateTime.UtcNow.AddHours(1) // Alpha Vantage resets hourly
        };
    }

    /// <summary>
    /// Updates predictions with actual results for backtesting
    /// </summary>
    public async Task UpdatePredictionResultsAsync(string symbol, DateTime targetDate, float actualReturn,
        float actualVolatility)
    {
        await repository.UpdateActualResultsAsync(symbol, targetDate, actualReturn, actualVolatility);
        logger.LogInformation("Updated actual results for {Symbol} on {Date}", symbol, targetDate);
    }
}
