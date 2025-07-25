using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.ML;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.StockPredictionModule.Load;
using TBD.StockPredictionModule.ML.Interface;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator;

namespace TBD.StockPredictionModule.ML;

public class MlStockPredictionEngine : IMlStockPredictionEngine
{
    private static readonly MLContext MlContext = new(seed: 0);
    private ITransformer? _model;
    private PredictionEngine<StockFeatureVector, StockPrediction>? _predictionEngine;

    // Use interface for basic counters (supports both text and OpenTelemetry)
    private readonly IMetricsService _metricsService;

    // Cast to OpenTelemetry service for histogram support
    private readonly OpenTelemetryMetricsService? _openTelemetryMetrics;

    public MlStockPredictionEngine(IMetricsServiceFactory metricsServiceFactory)
    {
        _metricsService = metricsServiceFactory.CreateMetricsService("StockPrediction");
        _openTelemetryMetrics = _metricsService as OpenTelemetryMetricsService;
    }

    // Keep static fields only for the highest-frequency metrics
    private static readonly Meter Meter = new("TBD.StockPrediction", "1.0.0");

    private static readonly Counter<int> PredictionAttempts =
        Meter.CreateCounter<int>("stock_prediction_attempts_total");

    private static readonly Histogram<double> PredictionDuration =
        Meter.CreateHistogram<double>("stock_prediction_duration_seconds", "seconds");

    public Task<bool> IsModelTrainedAsync()
    {
        _metricsService.IncrementCounter("stock.model_trained_checks_total");
        return Task.FromResult(_model != null && _predictionEngine != null);
    }

    public async Task TrainModelAsync(List<RawData> rawData)
    {
        var stopwatch = Stopwatch.StartNew();

        _metricsService.IncrementCounter("stock.train_model_attempts_total");
        Console.WriteLine("Starting model training...");

        try
        {
            if (rawData == null || rawData.Count == 0)
            {
                _metricsService.IncrementCounter("stock.train_model_failures_total");
                _metricsService.RecordHistogram("model is trained", stopwatch.ElapsedMilliseconds);
                throw new InvalidOperationException("No training data provided");
            }

            var beforeClean = rawData.Count;
            rawData = CleanTrainingData(rawData);
            var afterClean = rawData.Count;
            var removedRecords = beforeClean - afterClean;

            // Record data cleaning metrics
            _metricsService.IncrementCounter("stock.data_cleaning_records_removed_total");
            _openTelemetryMetrics?.RecordHistogram("stock.data_cleaning_records_removed", removedRecords);
            _openTelemetryMetrics?.RecordHistogram("stock.training_records_processed", afterClean);

            Console.WriteLine($"🧹 Cleaned training data: Removed {removedRecords} invalid records");

            if (rawData.Count == 0)
            {
                _metricsService.IncrementCounter("stock.train_model_failures_total");
                throw new InvalidOperationException("No valid training data after cleaning");
            }

            var features = FeatureEngineering.GenerateFeatures(rawData);

            Console.WriteLine($"Generated {features.Count} training feature rows");

            var trainTestSplit =
                MlContext.Data.TrainTestSplit(MlContext.Data.LoadFromEnumerable(features), testFraction: 0.2);

            var pipeline = MlContext.Transforms.Concatenate("Features",
                    nameof(StockFeatureVector.Open),
                    nameof(StockFeatureVector.High),
                    nameof(StockFeatureVector.Low),
                    nameof(StockFeatureVector.Close),
                    nameof(StockFeatureVector.Volume),
                    nameof(StockFeatureVector.MA5),
                    nameof(StockFeatureVector.MA10),
                    nameof(StockFeatureVector.Volatility5),
                    nameof(StockFeatureVector.Return1D))
                .Append(MlContext.Transforms.NormalizeMinMax("Features"))
                .Append(MlContext.Regression.Trainers.FastTree(
                    labelColumnName: nameof(StockFeatureVector.NextClose),
                    featureColumnName: "Features"));

            _model = pipeline.Fit(trainTestSplit.TrainSet);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            _predictionEngine = MlContext.Model.CreatePredictionEngine<StockFeatureVector, StockPrediction>(_model);

            _metricsService.IncrementCounter("stock.train_model_successes_total");
            Console.WriteLine("✅ Model training complete");

            // Model evaluation metrics
            var predictions = _model.Transform(trainTestSplit.TestSet);
            var metrics =
                MlContext.Regression.Evaluate(predictions, labelColumnName: nameof(StockFeatureVector.NextClose));

            _openTelemetryMetrics?.RecordHistogram("stock.model_rmse", metrics.RootMeanSquaredError);
            _openTelemetryMetrics?.RecordHistogram("stock.model_r_squared", metrics.RSquared);

            Console.WriteLine($"📊 Evaluation RMSE: {metrics.RootMeanSquaredError:F2}, R²: {metrics.RSquared:P2}");

            await Task.CompletedTask;
        }
        catch (Exception)
        {
            _metricsService.IncrementCounter("stock.train_model_failures_total");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            _openTelemetryMetrics?.RecordHistogram("stock.model_training_duration_seconds",
                stopwatch.Elapsed.TotalSeconds);
        }
    }

    // NEW: Streaming training method
    public async Task TrainModelStreamingAsync(string csvFilePath)
    {
        Console.WriteLine("🧠 Training model with streaming data...");

        var trainingData = new List<RawData>();
        var batchCount = 0;
        const int maxTrainingRecords = 50000; // Limit training data size

        await foreach (var batch in LoadCsvData.LoadRawDataBatchedAsync(csvFilePath, batchSize: 1000))
        {
            batchCount++;
            trainingData.AddRange(batch);

            Console.WriteLine($"Training batch {batchCount}: {batch.Count} records (Total: {trainingData.Count})");

            // Stop if we have enough training data
            if (trainingData.Count < maxTrainingRecords)
            {
                continue;
            }

            Console.WriteLine($"Reached training limit of {maxTrainingRecords:N0} records");
            trainingData = trainingData.Take(maxTrainingRecords).ToList();
            break;
        }

        Console.WriteLine($"Training model with {trainingData.Count:N0} records...");
        await TrainModelAsync(trainingData);

        // Clean up training data
        trainingData.Clear();
        GC.Collect();
    }

    // Original GeneratePredictAsync method
    // public Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol)
    // {
    //     var stopwatch = Stopwatch.StartNew();
    //
    //     // Use static counter for high-frequency metric
    //     PredictionAttempts.Add(1);
    //
    //     try
    //     {
    //         if (string.IsNullOrWhiteSpace(symbol))
    //         {
    //             _metricsService.IncrementCounter("stock.prediction_failures_total");
    //             throw new ArgumentException("Symbol is required", nameof(symbol));
    //         }
    //
    //         if (_predictionEngine == null)
    //         {
    //             _metricsService.IncrementCounter("stock.prediction_failures_total");
    //             throw new InvalidOperationException("Model must be trained before predictions");
    //         }
    //
    //         var ordered = rawData
    //             .Where(r => r.Symbol == symbol && r.Close > 0)
    //             .OrderBy(r => DateTime.Parse(r.Date))
    //             .ToList();
    //
    //         if (ordered.Count < 11)
    //         {
    //             _metricsService.IncrementCounter("stock.prediction_failures_total");
    //             throw new InvalidOperationException("Not enough data for feature generation");
    //         }
    //
    //         return Task.FromResult(GeneratePredictionFromData(ordered, symbol));
    //     }
    //     catch (Exception)
    //     {
    //         _metricsService.IncrementCounter("stock.prediction_failures_total");
    //         throw;
    //     }
    //     finally
    //     {
    //         stopwatch.Stop();
    //         // Use static histogram for high-frequency metric
    //         PredictionDuration.Record(stopwatch.Elapsed.TotalSeconds,
    //             new KeyValuePair<string, object?>("symbol", symbol));
    //     }
    // }

    // NEW: Generate prediction from grouped data
    public async Task<StockPrediction> GeneratePredictAsync(Dictionary<string, List<RawData>> groupedData,
        string symbol)
    {
        if (!groupedData.TryGetValue(symbol, out var symbolData))
        {
            throw new ArgumentException($"No data found for symbol: {symbol}");
        }

        return await GeneratePredictAsync(symbolData, symbol);
    }

    // NEW: Generate prediction with just symbol data (memory efficient)
    public Task<StockPrediction> GeneratePredictAsync(List<RawData> symbolData, string symbol)
    {
        var stopwatch = Stopwatch.StartNew();
        PredictionAttempts.Add(1);

        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                _metricsService.IncrementCounter("stock.prediction_failures_total");
                throw new ArgumentException("Symbol is required", nameof(symbol));
            }

            if (_predictionEngine == null)
            {
                _metricsService.IncrementCounter("stock.prediction_failures_total");
                throw new InvalidOperationException("Model must be trained before predictions");
            }

            if (symbolData.Count < 11)
            {
                _metricsService.IncrementCounter("stock.prediction_failures_total");
                throw new InvalidOperationException(
                    $"Not enough data for {symbol}: {symbolData.Count} records (need at least 11)");
            }

            // Data should already be sorted, but ensure it is
            var ordered = symbolData
                .Where(r => r.Close > 0)
                .OrderBy(r => DateTime.Parse(r.Date))
                .ToList();

            if (ordered.Count >= 11)
            {
                return Task.FromResult(GeneratePredictionFromData(ordered, symbol));
            }

            _metricsService.IncrementCounter("stock.prediction_failures_total");
            throw new InvalidOperationException(
                $"Not enough valid data for {symbol} after filtering: {ordered.Count} records");
        }
        catch (Exception)
        {
            _metricsService.IncrementCounter("stock.prediction_failures_total");
            throw;
        }
        finally
        {
            stopwatch.Stop();
            PredictionDuration.Record(stopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("symbol", symbol));
        }
    }

    // Helper method to generate prediction from ordered data
    private StockPrediction GeneratePredictionFromData(List<RawData> ordered, string symbol)
    {
        var i = ordered.Count - 1;
        var window5 = ordered.Skip(i - 4).Take(5).ToList();
        var window10 = ordered.Skip(i - 9).Take(10).ToList();
        var today = ordered[i];

        var input = new StockFeatureVector
        {
            Open = today.Open,
            High = today.High,
            Low = today.Low,
            Close = today.Close,
            Volume = today.Volume,
            MA5 = window5.Average(x => x.Close),
            MA10 = window10.Average(x => x.Close),
            Volatility5 =
                (float)Math.Sqrt(window5.Average(x => Math.Pow(x.Close - window5.Average(w => w.Close), 2))),
            Return1D = (today.Close - ordered[i - 1].Close) / ordered[i - 1].Close
        };

        var predicted = _predictionEngine!.Predict(input);

        _metricsService.IncrementCounter("stock.prediction_successes_total");

        // Record predicted price with symbol tag for filtering in Prometheus
        _openTelemetryMetrics?.RecordHistogram("stock.predicted_price", predicted.PredictedPrice,
            new KeyValuePair<string, object?>("symbol", symbol));

        var result = new StockPrediction
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            BatchId = Guid.NewGuid(),
            PredictedPrice = Math.Max(0.01f, predicted.PredictedPrice),
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        Console.WriteLine($"🔮 {symbol}: Predicted next close = ${result.PredictedPrice:F2}");

        return result;
    }

    public List<RawData> CleanTrainingData(List<RawData> rawData)
    {
        return rawData.Where(r =>
            r is { Open: > 0, High: > 0, Low: > 0, Close: > 0, Volume: > 0 } &&
            r.High >= r.Low &&
            r.High >= r.Open &&
            r.High >= r.Close &&
            r.Low <= r.Open &&
            r.Low <= r.Close &&
            !string.IsNullOrEmpty(r.Symbol) &&
            DateTime.TryParse(r.Date, out _)
        ).ToList();
    }
}
