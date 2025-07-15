using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.ML;
using TBD.MetricsModule.Services.Interfaces;
using TBD.StockPredictionModule.ML.Interface;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator;

namespace TBD.StockPredictionModule.ML;

public class MlStockPredictionEngine(IMetricsServiceFactory metricsServiceFactory)
    : IMlStockPredictionEngine
{
    private static readonly MLContext MlContext = new(seed: 0);
    private ITransformer? _model;
    private PredictionEngine<StockFeatureVector, StockPrediction>? _predictionEngine;
    private readonly IMetricsService _metricsService = metricsServiceFactory.CreateMetricsService("StockPrediction");

    // OpenTelemetry metrics
    private static readonly Meter Meter = new("TBD.StockPrediction", "1.0.0");

    private static readonly Counter<int> ModelTrainedChecks =
        Meter.CreateCounter<int>("stock_model_trained_checks_total", "Total number of model trained checks");

    private static readonly Counter<int> ModelTrainAttempts =
        Meter.CreateCounter<int>("stock_model_train_attempts_total", "Total number of model training attempts");

    private static readonly Counter<int> ModelTrainSuccesses =
        Meter.CreateCounter<int>("stock_model_train_successes_total", "Total number of successful model trainings");

    private static readonly Counter<int> ModelTrainFailures =
        Meter.CreateCounter<int>("stock_model_train_failures_total", "Total number of failed model trainings");

    private static readonly Counter<int> PredictionAttempts =
        Meter.CreateCounter<int>("stock_prediction_attempts_total", "Total number of prediction attempts");

    private static readonly Counter<int> PredictionSuccesses =
        Meter.CreateCounter<int>("stock_prediction_successes_total", "Total number of successful predictions");

    private static readonly Counter<int> PredictionFailures =
        Meter.CreateCounter<int>("stock_prediction_failures_total", "Total number of failed predictions");

    private static readonly Counter<int> DataCleaningRecordsRemoved =
        Meter.CreateCounter<int>("stock_data_cleaning_records_removed_total",
            "Total number of records removed during data cleaning");

    private static readonly Histogram<double> ModelTrainingDuration =
        Meter.CreateHistogram<double>("stock_model_training_duration_seconds", "Duration of model training in seconds");

    private static readonly Histogram<double> PredictionDuration =
        Meter.CreateHistogram<double>("stock_prediction_duration_seconds",
            "Duration of prediction generation in seconds");

    private static readonly Histogram<double> ModelRmse =
        Meter.CreateHistogram<double>("stock_model_rmse", "Root Mean Square Error of the trained model");

    private static readonly Histogram<double> ModelRSquared =
        Meter.CreateHistogram<double>("stock_model_r_squared", "R-squared value of the trained model");

    private static readonly Histogram<double> PredictedPrice =
        Meter.CreateHistogram<double>("stock_predicted_price", "Predicted stock prices");

    private static readonly Gauge<int> TrainingRecordsProcessed =
        Meter.CreateGauge<int>("stock_training_records_processed", "Number of records processed in last training");

    private static readonly Gauge<int> FeatureVectorsGenerated =
        Meter.CreateGauge<int>("stock_feature_vectors_generated",
            "Number of feature vectors generated in last training");

    public Task<bool> IsModelTrainedAsync()
    {
        _metricsService.IncrementCounter("stock.is_model_trained_check");
        ModelTrainedChecks.Add(1);

        return Task.FromResult(_model != null && _predictionEngine != null);
    }

    public async Task TrainModelAsync(List<RawData> rawData)
    {
        var stopwatch = Stopwatch.StartNew();

        _metricsService.IncrementCounter("stock.train_model.invoked");
        ModelTrainAttempts.Add(1);
        Console.WriteLine("Starting model training...");

        try
        {
            if (rawData == null || rawData.Count == 0)
            {
                _metricsService.IncrementCounter("stock.train_model.empty_data");
                ModelTrainFailures.Add(1, new KeyValuePair<string, object?>("reason", "empty_data"));
                throw new InvalidOperationException("No training data provided");
            }

            var beforeClean = rawData.Count;
            rawData = CleanTrainingData(rawData);
            var afterClean = rawData.Count;
            var removedRecords = beforeClean - afterClean;

            _metricsService.IncrementCounter($"stock.train_model.cleaned_record_count{afterClean}");
            _metricsService.IncrementCounter($"stock.train_model.rejected_record_count{removedRecords}");

            DataCleaningRecordsRemoved.Add(removedRecords);
            TrainingRecordsProcessed.Record(afterClean);

            Console.WriteLine($"🧹 Cleaned training data: Removed {removedRecords} invalid records");

            if (rawData.Count == 0)
            {
                _metricsService.IncrementCounter("stock.train_model.all_data_rejected");
                ModelTrainFailures.Add(1, new KeyValuePair<string, object?>("reason", "all_data_rejected"));
                throw new InvalidOperationException("No valid training data after cleaning");
            }

            var features = FeatureEngineering.GenerateFeatures(rawData);
            _metricsService.IncrementCounter($"stock.train_model.feature_count{features.Count}");

            FeatureVectorsGenerated.Record(features.Count);

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

            _metricsService.IncrementCounter("stock.train_model.success");
            ModelTrainSuccesses.Add(1);
            Console.WriteLine("✅ Model training complete");

            // Optional evaluation
            var predictions = _model.Transform(trainTestSplit.TestSet);
            var metrics =
                MlContext.Regression.Evaluate(predictions, labelColumnName: nameof(StockFeatureVector.NextClose));

            _metricsService.IncrementCounter($"stock.train_model.rmse{(float)metrics.RootMeanSquaredError}");
            _metricsService.IncrementCounter($"stock.train_model.rsquared{(float)metrics.RSquared}");

            // Record OpenTelemetry metrics
            ModelRmse.Record(metrics.RootMeanSquaredError);
            ModelRSquared.Record(metrics.RSquared);

            Console.WriteLine($"📊 Evaluation RMSE: {metrics.RootMeanSquaredError:F2}, R²: {metrics.RSquared:P2}");

            await Task.CompletedTask;
        }
        catch (Exception)
        {
            ModelTrainFailures.Add(1, new KeyValuePair<string, object?>("reason", "exception"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            ModelTrainingDuration.Record(stopwatch.Elapsed.TotalSeconds);
        }
    }

    public Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol)
    {
        var stopwatch = Stopwatch.StartNew();

        _metricsService.IncrementCounter("stock.prediction.attempt");
        PredictionAttempts.Add(1);

        try
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                _metricsService.IncrementCounter("stock.prediction.symbol_missing");
                PredictionFailures.Add(1,
                    new KeyValuePair<string, object?>("reason", "symbol_missing"));
                throw new ArgumentException("Symbol is required", nameof(symbol));
            }

            if (_predictionEngine == null)
            {
                _metricsService.IncrementCounter("stock.prediction.model_untrained");
                PredictionFailures.Add(1,
                    new KeyValuePair<string, object?>("reason", "model_untrained"));
                throw new InvalidOperationException("Model must be trained before predictions");
            }

            var ordered = rawData
                .Where(r => r.Symbol == symbol && r.Close > 0)
                .OrderBy(r => DateTime.Parse(r.Date))
                .ToList();

            if (ordered.Count < 11)
            {
                _metricsService.IncrementCounter("stock.prediction.insufficient_data");
                PredictionFailures.Add(1,
                    new KeyValuePair<string, object?>("reason", "insufficient_data"));
                throw new InvalidOperationException("Not enough data for feature generation");
            }

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

            var predicted = _predictionEngine.Predict(input);

            _metricsService.IncrementCounter($"stock.prediction.predicted_price{predicted.PredictedPrice:F2}");
            _metricsService.IncrementCounter("stock.prediction.success");

            PredictionSuccesses.Add(1);
            PredictedPrice.Record(predicted.PredictedPrice,
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

            return Task.FromResult(result);
        }
        catch (Exception)
        {
            PredictionFailures.Add(1, new KeyValuePair<string, object?>("reason", "exception"));
            throw;
        }
        finally
        {
            stopwatch.Stop();
            PredictionDuration.Record(stopwatch.Elapsed.TotalSeconds);
        }
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
