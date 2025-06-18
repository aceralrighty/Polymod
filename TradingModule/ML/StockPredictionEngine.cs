using Microsoft.ML;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Preprocessing;

namespace TBD.TradingModule.ML;

public class StockPredictionEngine(ILogger<StockPredictionEngine> logger)
{
    private readonly MLContext _mlContext = new(seed: 42);
    private ITransformer? _model;
    private PredictionEngine<StockFeatureVector, StockDirectionPrediction>? _predictionEngine;

    private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "StockPredictionModel.zip");
    private const string ModelVersion = "v1.0";

    /// <summary>
    /// Train model and generate predictions from engineered feature sets.
    /// </summary>
    public async Task<List<PredictionResult>> TrainAndPredictAsync(
        IEnumerable<FeatureEngineeringService.FeatureSet> sets)
    {
        var featureSets = sets as FeatureEngineeringService.FeatureSet[] ?? sets.ToArray();
        var vectors = featureSets.Select(f => f.Vector).Where(v => v.NextDayReturn.HasValue).ToList();

        if (ShouldRetrain())
        {
            logger.LogInformation("Training model with {Count} vectors", vectors.Count);
            TrainAndSaveModel(vectors);
        }
        else
        {
            logger.LogInformation("Using cached model for predictions");
            LoadModelIfNeeded();
        }

        var predictions = featureSets.Select(set => Predict(set.Vector)).ToList();

        return await Task.FromResult(predictions);
    }

    /// <summary>
    /// Predict using existing trained model only.
    /// </summary>
    public async Task<List<PredictionResult>> PredictOnlyAsync(IEnumerable<StockFeatureVector> inputs)
    {
        LoadModelIfNeeded();

        if (_predictionEngine == null)
            throw new InvalidOperationException("No trained model available. Please train the model first.");

        var results = inputs.Select(Predict).ToList();
        return await Task.FromResult(results);
    }

    /// <summary>
    /// Retrains and saves model using raw vectors.
    /// </summary>
    public async Task RetrainModelAsync(IEnumerable<StockFeatureVector> vectors)
    {
        TrainAndSaveModel(vectors.Where(v => v.NextDayReturn.HasValue).ToList());
        await Task.CompletedTask;
    }

    private void TrainAndSaveModel(IEnumerable<StockFeatureVector> data)
    {
        var processedData = data.Select(f =>
        {
            f.NextDayReturn = f.NextDayReturn > 0 ? 1f : 0f;
            return f;
        }).ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(processedData);

        var pipeline = _mlContext.Transforms.Concatenate("Features",
                nameof(StockFeatureVector.PriceReturn1Day),
                nameof(StockFeatureVector.PriceReturn5Day),
                nameof(StockFeatureVector.PriceReturn20Day),
                nameof(StockFeatureVector.MA5Ratio),
                nameof(StockFeatureVector.MA10Ratio),
                nameof(StockFeatureVector.MA20Ratio),
                nameof(StockFeatureVector.MA50Ratio),
                nameof(StockFeatureVector.RSI),
                nameof(StockFeatureVector.MACD),
                nameof(StockFeatureVector.MACDSignal),
                nameof(StockFeatureVector.BollingerPosition),
                nameof(StockFeatureVector.VolumeRatio20Day),
                nameof(StockFeatureVector.VolumeRatioMA),
                nameof(StockFeatureVector.Volatility20Day),
                nameof(StockFeatureVector.HighLowRatio),
                nameof(StockFeatureVector.MarketBeta),
                nameof(StockFeatureVector.SectorPerformance)
            )
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(StockFeatureVector.NextDayReturn)));

        _model = pipeline.Fit(dataView);
        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

        _predictionEngine = _mlContext.Model
            .CreatePredictionEngine<StockFeatureVector, StockDirectionPrediction>(_model);

        logger.LogInformation("Model trained and saved to {Path}", _modelPath);
    }

    private PredictionResult Predict(StockFeatureVector input)
    {
        LoadModelIfNeeded();
        var prediction = _predictionEngine!.Predict(input);

        var assumedVolatility = input.NextDayVolatility ?? 0.02f;
        var assumedReturn = prediction.PredictedLabel ? 0.01f : -0.005f;

        return new PredictionResult
        {
            Symbol = input.Symbol,
            PredictionDate = DateTime.UtcNow,
            TargetDate = input.Date.AddDays(1),
            PredictedReturn = assumedReturn,
            PredictedVolatility = assumedVolatility,
            ConfidenceScore = prediction.Probability,
            RiskAdjustedScore = assumedReturn / assumedVolatility,
            ModelVersion = ModelVersion,
            CreatedAt = DateTime.UtcNow
        };
    }

    private void LoadModelIfNeeded()
    {
        if (_model != null || !File.Exists(_modelPath))
            return;

        using var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read);
        _model = _mlContext.Model.Load(stream, out _);
        _predictionEngine = _mlContext.Model
            .CreatePredictionEngine<StockFeatureVector, StockDirectionPrediction>(_model);
    }

    private bool ShouldRetrain()
    {
        if (!File.Exists(_modelPath)) return true;
        var modelAge = DateTime.UtcNow - File.GetLastWriteTimeUtc(_modelPath);
        return modelAge > TimeSpan.FromDays(7);
    }
}
