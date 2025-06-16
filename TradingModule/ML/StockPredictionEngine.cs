using Microsoft.ML;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.Preprocessing;

namespace TBD.TradingModule.ML;

public class StockPredictionEngine
{
    private readonly MLContext _mlContext = new(seed: 42);
    private ITransformer? _model;
    private PredictionEngine<StockFeatureVector, StockDirectionPrediction>? _predictionEngine;

    private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "Models", "StockPredictionModel.zip");
    private const string ModelVersion = "v1.0";

    private void TrainAndSaveModel(IEnumerable<StockFeatureVector> data)
    {
        var trainingData = data
            .Where(f => f.NextDayReturn.HasValue)
            .Select(f =>
            {
                f.NextDayReturn = f.NextDayReturn > 0 ? 1f : 0f;
                return f;
            })
            .ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

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
    }

    private void LoadModelIfNeeded()
    {
        if (_model != null || !File.Exists(_modelPath))
        {
            return;
        }

        using var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read);
        _model = _mlContext.Model.Load(stream, out _);
        _predictionEngine =
            _mlContext.Model.CreatePredictionEngine<StockFeatureVector, StockDirectionPrediction>(_model);
    }

    private PredictionResult Predict(StockFeatureVector input)
    {
        LoadModelIfNeeded();
        var prediction = _predictionEngine!.Predict(input);

        var assumedVolatility = input.NextDayVolatility ?? 0.02f; // fallback
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

    /// <summary>
    /// Train model and generate predictions - returns predictions instead of saving directly
    /// </summary>
    public async Task<List<PredictionResult>> TrainAndPredictFromFeatureSetsAsync(
        IEnumerable<FeatureEngineeringService.FeatureSet> sets)
    {
        var featureSets = sets as FeatureEngineeringService.FeatureSet[] ?? sets.ToArray();
        var featureVectors = featureSets.Select(f => f.Vector).ToList();

        // Only retrain if necessary
        if (ShouldRetrain())
        {
            TrainAndSaveModel(featureVectors);
        }

        var predictions = new List<PredictionResult>();
        foreach (var fs in featureSets)
        {
            var result = Predict(fs.Vector);
            predictions.Add(result);
        }

        return await Task.FromResult(predictions);
    }

    /// <summary>
    /// Generate predictions without training (assumes model exists)
    /// </summary>
    public async Task<List<PredictionResult>> PredictFromFeatureVectorsAsync(
        IEnumerable<StockFeatureVector> inputs)
    {
        LoadModelIfNeeded();

        if (_predictionEngine == null)
        {
            throw new InvalidOperationException("No trained model available. Please train the model first.");
        }

        var predictions = new List<PredictionResult>();
        foreach (var input in inputs)
        {
            var result = Predict(input);
            predictions.Add(result);
        }

        return await Task.FromResult(predictions);
    }

    private bool ShouldRetrain()
    {
        if (!File.Exists(_modelPath)) return true;

        var modelAge = DateTime.UtcNow - File.GetLastWriteTime(_modelPath);
        return modelAge > TimeSpan.FromDays(7); // Retrain weekly
    }

    /// <summary>
    /// Force retrain the model with new data
    /// </summary>
    public async Task RetrainModelAsync(IEnumerable<StockFeatureVector> trainingData)
    {
        TrainAndSaveModel(trainingData);
        await Task.CompletedTask;
    }
}
