using System.Diagnostics;
using System.Diagnostics.Metrics;
using TBD.MetricsModule.Services.Interfaces;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.Shared.EntityMappers;
using TBD.StockPredictionModule.Load;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator.Interface;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.PipelineOrchestrator;

public class StockPredictionPipeline : IStockPredictionPipeline
{
    private readonly StockEntityMapper _entityMapper;
    private readonly MlStockPredictionEngine _mlEngine;
    private readonly IStockPredictionRepository _stockPredictionRepository;
    private readonly IStockRepository _stockRepository;
    private readonly IMetricsService _metricsService;
    private readonly OpenTelemetryMetricsService? _openTelemetryMetrics;

    private static readonly Meter Meter = new("TBD.StockPipeline", "1.0.0");

    private static readonly Counter<int> PipelineExecutions =
        Meter.CreateCounter<int>("stock_pipeline_executions_total");

    private static readonly Counter<int> PredictionGenerationAttempts =
        Meter.CreateCounter<int>("stock_pipeline_prediction_attempts_total");

    private static readonly Histogram<double> PipelineExecutionDuration =
        Meter.CreateHistogram<double>("stock_pipeline_execution_duration_seconds", "seconds");

    private static readonly Histogram<double> AccuracyCheckDuration =
        Meter.CreateHistogram<double>("stock_pipeline_accuracy_check_duration_seconds", "seconds");


    public StockPredictionPipeline(
        StockEntityMapper entityMapper,
        MlStockPredictionEngine mlEngine,
        IStockPredictionRepository stockPredictionRepository,
        IStockRepository stockRepository,
        IMetricsServiceFactory metricsServiceFactory)
    {
        _entityMapper = entityMapper;
        _mlEngine = mlEngine;
        _stockPredictionRepository = stockPredictionRepository;
        _stockRepository = stockRepository;
        _metricsService = metricsServiceFactory.CreateMetricsService("StockPipeline");
        _openTelemetryMetrics = _metricsService as OpenTelemetryMetricsService;
    }


    public async Task<List<StockPrediction>> ExecuteFullPipelineAsync(string csvFilePath)
    {
        var pipelineStopwatch = Stopwatch.StartNew();
        var batchId = Guid.NewGuid();

        PipelineExecutions.Add(1);
        _metricsService.IncrementCounter("stock.pipeline_executions_total");

        try
        {
            Console.WriteLine("Step 1: Getting record count...");
            var totalRecords = await LoadCsvData.GetRecordCountAsync(csvFilePath);
            Console.WriteLine($"Found {totalRecords:N0} records in CSV file");

            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_total_records_found", totalRecords);

            Console.WriteLine("Step 2: Processing data in streaming batches...");
            var groupedBySymbol = new Dictionary<string, List<RawData>>();
            var batchNumber = 0;
            var totalProcessed = 0;

            // Process in true streaming fashion - only keep grouped data, not all raw data
            await foreach (var batch in LoadCsvData.LoadRawDataBatchedAsync(csvFilePath, batchSize: 2000))
            {
                batchNumber++;
                totalProcessed += batch.Count;
                Console.WriteLine(
                    $"Processing batch {batchNumber}: {batch.Count:N0} records (Total: {totalProcessed:N0})");

                // Group by symbol as we go, but don't keep the original batch
                foreach (var record in batch)
                {
                    if (!groupedBySymbol.ContainsKey(record.Symbol))
                    {
                        groupedBySymbol[record.Symbol] = new List<RawData>();
                    }

                    groupedBySymbol[record.Symbol].Add(record);
                }

                _openTelemetryMetrics?.RecordHistogram("stock.pipeline_batch_processed", batch.Count);

                // Force garbage collection every 10 batches to manage memory
                if (batchNumber % 10 != 0)
                {
                    continue;
                }

                GC.Collect();
                GC.WaitForPendingFinalizers();
                var memoryUsed = GC.GetTotalMemory(false) / (1024.0 * 1024.0);
                Console.WriteLine($"    Memory check at batch {batchNumber}: {memoryUsed:F1} MB");
            }

            // Sort each symbol's data by date efficiently
            Console.WriteLine("Step 2.5: Sorting symbol data by date...");
            var sortStopwatch = Stopwatch.StartNew();

            foreach (var symbol in groupedBySymbol.Keys.ToList())
            {
                groupedBySymbol[symbol].Sort((a, b) => DateTime.Parse(a.Date).CompareTo(DateTime.Parse(b.Date)));
            }

            sortStopwatch.Stop();
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_sort_duration_seconds",
                sortStopwatch.Elapsed.TotalSeconds);

            Console.WriteLine($"Processed {totalProcessed:N0} records across {groupedBySymbol.Count} symbols");
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_unique_symbols_found", groupedBySymbol.Count);

            Console.WriteLine("Step 3: Training model with streaming data...");
            var trainStopwatch = Stopwatch.StartNew();

            // Train model using streaming approach instead of loading all data
            await _mlEngine.TrainModelStreamingAsync(csvFilePath);
            trainStopwatch.Stop();

            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_model_training_duration_seconds",
                trainStopwatch.Elapsed.TotalSeconds);
            _metricsService.IncrementCounter("stock.pipeline_model_training_completed_total");

            Console.WriteLine("Step 4: Quick accuracy check...");
            var avgAccuracy = await PerformQuickAccuracyCheck(groupedBySymbol);

            var allPredictions = new List<StockPrediction>();
            var successfulPredictions = 0;
            var failedPredictions = 0;

            Console.WriteLine("Step 5: Generating predictions for each symbol...");
            var predictionStopwatch = Stopwatch.StartNew();
            var symbolCount = 0;

            foreach (var (symbol, symbolRawData) in groupedBySymbol)
            {
                symbolCount++;

                try
                {
                    Console.WriteLine(
                        $"Processing {symbol} ({symbolCount}/{groupedBySymbol.Count}) - {symbolRawData.Count} records");

                    PredictionGenerationAttempts.Add(1);

                    var prediction = await _mlEngine.GeneratePredictAsync(groupedBySymbol, symbol);
                    prediction.BatchId = batchId;

                    allPredictions.Add(prediction);
                    successfulPredictions++;

                    _openTelemetryMetrics?.RecordHistogram("stock.pipeline_prediction_generated",
                        prediction.PredictedPrice,
                        new KeyValuePair<string, object?>("symbol", symbol));

                    Console.WriteLine($"✅ Prediction for {symbol}: ${prediction.PredictedPrice:F2}");
                }
                catch (Exception ex)
                {
                    failedPredictions++;
                    _metricsService.IncrementCounter("stock.pipeline_prediction_failures_total");
                    Console.WriteLine($"❌ Failed to process {symbol}: {ex.Message}");
                }
            }

            predictionStopwatch.Stop();
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_prediction_generation_duration_seconds",
                predictionStopwatch.Elapsed.TotalSeconds);
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_successful_predictions", successfulPredictions);
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_failed_predictions", failedPredictions);

            Console.WriteLine("Step 6: Transforming data to stocks in batches...");
            var transformStopwatch = Stopwatch.StartNew();
            var allStocks = new List<Stock>();

            // Transform in batches to avoid loading all data again
            var transformBatchNumber = 0;
            await foreach (var batch in LoadCsvData.LoadRawDataBatchedAsync(csvFilePath, batchSize: 2000))
            {
                transformBatchNumber++;
                var stockBatch = _entityMapper.TransformRawDataToStocks(batch);
                allStocks.AddRange(stockBatch);

                Console.WriteLine(
                    $"Transformed batch {transformBatchNumber}: {batch.Count} records -> {stockBatch.Count} stocks");

                // Clean up batch immediately
                if (transformBatchNumber % 5 == 0)
                {
                    GC.Collect();
                }
            }

            transformStopwatch.Stop();

            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_data_transformation_duration_seconds",
                transformStopwatch.Elapsed.TotalSeconds);
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_stocks_created", allStocks.Count);

            Console.WriteLine($"EntityMapper created {allStocks.Count} stocks from {totalProcessed} raw data records");

            Console.WriteLine($"Step 7: Saving {allPredictions.Count} predictions to database...");
            var saveStopwatch = Stopwatch.StartNew();

            await _stockPredictionRepository.SaveStockPredictionBatchAsync(allPredictions);
            await _stockRepository.SaveStockAsync(allStocks);

            saveStopwatch.Stop();
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_database_save_duration_seconds",
                saveStopwatch.Elapsed.TotalSeconds);
            _metricsService.IncrementCounter("stock.pipeline_database_saves_completed_total");

            _metricsService.IncrementCounter("stock.pipeline_executions_successful_total");
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_batch_id", batchId.GetHashCode());

            if (avgAccuracy.HasValue)
            {
                _openTelemetryMetrics?.RecordHistogram("stock.pipeline_final_accuracy_percentage", avgAccuracy.Value);
            }

            Console.WriteLine(
                $"Pipeline completed successfully! Generated predictions for {allPredictions.Count} symbols");
            return allPredictions;
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter("stock.pipeline_executions_failed_total");
            Console.WriteLine($"Error executing pipeline: {ex.Message}");
            throw;
        }
        finally
        {
            pipelineStopwatch.Stop();
            PipelineExecutionDuration.Record(pipelineStopwatch.Elapsed.TotalSeconds,
                new KeyValuePair<string, object?>("batch_id", batchId.ToString()));
        }
    }

    public async Task<double?> PerformQuickAccuracyCheck(Dictionary<string, List<RawData>> groupedBySymbol)
    {
        var accuracyStopwatch = Stopwatch.StartNew();
        _metricsService.IncrementCounter("stock.pipeline_accuracy_checks_total");

        try
        {
            var testSymbols = groupedBySymbol.Take(5).ToList();
            var totalError = 0.0;
            var testCount = 0;
            var accuracyTestsPerformed = 0;
            var accuracyTestsFailed = 0;

            foreach (var (symbol, historicalData) in testSymbols)
            {
                if (historicalData.Count < 10) continue;

                var actual = historicalData[^1];

                try
                {
                    accuracyTestsPerformed++;
                    var testData = historicalData.Take(historicalData.Count - 1).ToList();
                    var prediction = await _mlEngine.GeneratePredictAsync(testData, symbol);

                    var error = Math.Abs(prediction.PredictedPrice - actual.Close);
                    var percentageError = (error / actual.Close) * 100;

                    totalError += percentageError;
                    testCount++;

                    _openTelemetryMetrics?.RecordHistogram("stock.pipeline_accuracy_test_error_percentage",
                        percentageError,
                        new KeyValuePair<string, object?>("symbol", symbol));
                    _openTelemetryMetrics?.RecordHistogram("stock.pipeline_accuracy_test_predicted_price",
                        prediction.PredictedPrice,
                        new KeyValuePair<string, object?>("symbol", symbol));
                    _openTelemetryMetrics?.RecordHistogram("stock.pipeline_accuracy_test_actual_price", actual.Close,
                        new KeyValuePair<string, object?>("symbol", symbol));

                    Console.WriteLine(
                        $"   {symbol}: Predicted ${prediction.PredictedPrice:F2}, Actual ${actual.Close:F2}, Error: {percentageError:F1}%");
                }
                catch (Exception ex)
                {
                    accuracyTestsFailed++;
                    _metricsService.IncrementCounter("stock.pipeline_accuracy_test_failures_total");
                    Console.WriteLine($"   {symbol}: Could not test accuracy - {ex.Message}");
                }
            }

            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_accuracy_tests_performed", accuracyTestsPerformed);
            _openTelemetryMetrics?.RecordHistogram("stock.pipeline_accuracy_tests_failed", accuracyTestsFailed);

            if (testCount > 0)
            {
                var avgError = totalError / testCount;
                var accuracyRating = avgError < 5 ? "🟢 GOOD" : avgError < 10 ? "🟡 FAIR" : "🔴 POOR";

                _openTelemetryMetrics?.RecordHistogram("stock.pipeline_average_accuracy_error_percentage", avgError);
                _metricsService.IncrementCounter("stock.pipeline_accuracy_checks_successful_total");

                Console.WriteLine($"📊 Average prediction error: {avgError:F1}% - {accuracyRating}");
                return avgError;
            }
            else
            {
                _metricsService.IncrementCounter("stock.pipeline_accuracy_checks_failed_total");
                Console.WriteLine("⚠️ Could not perform accuracy check");
                return null;
            }
        }
        catch (Exception ex)
        {
            _metricsService.IncrementCounter("stock.pipeline_accuracy_checks_failed_total");
            Console.WriteLine($"Error during accuracy check: {ex.Message}");
            return null;
        }
        finally
        {
            accuracyStopwatch.Stop();
            AccuracyCheckDuration.Record(accuracyStopwatch.Elapsed.TotalSeconds);
        }
    }
}
