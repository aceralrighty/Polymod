using Microsoft.ML;
using TBD.MetricsModule.Services;
using TBD.StockPredictionModule.ML.Interface;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator;

namespace TBD.StockPredictionModule.ML;

public class MlStockPredictionEngine(IMetricsServiceFactory metricsServiceFactory, ITransformer? model)
    : IMlStockPredictionEngine
{
    private static readonly MLContext MlContext = new(seed: 0);
    private ITransformer? _model = model;
    private PredictionEngine<StockFeatureVector, StockPrediction>? _predictionEngine;
    private readonly IMetricsService _metricsService = metricsServiceFactory.CreateMetricsService("StockPrediction");

    public Task<bool> IsModelTrainedAsync()
    {
        _metricsService.IncrementCounter("stock.is_model_trained_check");
        return Task.FromResult(_model != null && _predictionEngine != null);
    }

    public async Task TrainModelAsync(List<RawData> rawData)
    {
        _metricsService.IncrementCounter("stock.train_model.invoked");
        Console.WriteLine("Starting model training...");

        if (rawData == null || rawData.Count == 0)
        {
            _metricsService.IncrementCounter("stock.train_model.empty_data");
            throw new InvalidOperationException("No training data provided");
        }

        var beforeClean = rawData.Count;
        rawData = CleanTrainingData(rawData);
        var afterClean = rawData.Count;

        _metricsService.IncrementCounter($"stock.train_model.cleaned_record_count{afterClean}");
        _metricsService.IncrementCounter($"stock.train_model.rejected_record_count{beforeClean - afterClean}");
        Console.WriteLine($"🧹 Cleaned training data: Removed {beforeClean - afterClean} invalid records");

        if (rawData.Count == 0)
        {
            _metricsService.IncrementCounter("stock.train_model.all_data_rejected");
            throw new InvalidOperationException("No valid training data after cleaning");
        }

        var features = FeatureEngineering.GenerateFeatures(rawData);
        _metricsService.IncrementCounter($"stock.train_model.feature_count{features.Count}");
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
        Console.WriteLine("✅ Model training complete");

        // Optional evaluation
        var predictions = _model.Transform(trainTestSplit.TestSet);
        var metrics = MlContext.Regression.Evaluate(predictions, labelColumnName: nameof(StockFeatureVector.NextClose));

        _metricsService.IncrementCounter($"stock.train_model.rmse{(float)metrics.RootMeanSquaredError}");
        _metricsService.IncrementCounter($"stock.train_model.rsquared{(float)metrics.RSquared}");
        Console.WriteLine($"📊 Evaluation RMSE: {metrics.RootMeanSquaredError:F2}, R²: {metrics.RSquared:P2}");

        await Task.CompletedTask;
    }

    public Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol)
    {
        _metricsService.IncrementCounter("stock.prediction.attempt");

        if (string.IsNullOrWhiteSpace(symbol))
        {
            _metricsService.IncrementCounter("stock.prediction.symbol_missing");
            throw new ArgumentException("Symbol is required", nameof(symbol));
        }

        if (_predictionEngine == null)
        {
            _metricsService.IncrementCounter("stock.prediction.model_untrained");
            throw new InvalidOperationException("Model must be trained before predictions");
        }

        var ordered = rawData
            .Where(r => r.Symbol == symbol && r.Close > 0)
            .OrderBy(r => DateTime.Parse(r.Date))
            .ToList();

        if (ordered.Count < 11)
        {
            _metricsService.IncrementCounter("stock.prediction.insufficient_data");
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
            Volatility5 = (float)Math.Sqrt(window5.Average(x => Math.Pow(x.Close - window5.Average(w => w.Close), 2))),
            Return1D = (today.Close - ordered[i - 1].Close) / ordered[i - 1].Close
        };

        var predicted = _predictionEngine.Predict(input);

        _metricsService.IncrementCounter($"stock.prediction.predicted_price{predicted.Price:F2}");
        _metricsService.IncrementCounter("stock.prediction.success");

        var result = new StockPrediction
        {
            Id = Guid.NewGuid(),
            Symbol = symbol,
            BatchId = Guid.NewGuid(),
            Price = Math.Max(0.01f, predicted.Price),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        Console.WriteLine($"🔮 {symbol}: Predicted next close = ${result.Price:F2}");

        return Task.FromResult(result);
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
