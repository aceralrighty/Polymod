using Microsoft.ML;
using TBD.TradingModule.DataAccess;
using TBD.TradingModule.MarketData;
using TBD.TradingModule.Model;
using TBD.TradingModule.Services;

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

    public async Task PredictAndSaveAsync(
        IEnumerable<StockFeatureVector> inputs,
        TradingDbContext dbContext)
    {
        foreach (var input in inputs)
        {
            var result = Predict(input);
            dbContext.Predictions.Add(result);
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task TrainAndPredictFromFeatureSetsAsync(
        IEnumerable<FeatureEngineeringService.FeatureSet> sets,
        TradingDbContext dbContext)
    {
        var featureSets = sets as FeatureEngineeringService.FeatureSet[] ?? sets.ToArray();
        var featureVectors = featureSets.Select(f => f.Vector).ToList();
        TrainAndSaveModel(featureVectors);

        foreach (var fs in featureSets)
        {
            var result = Predict(fs.Vector);
            dbContext.Predictions.Add(result);
        }

        await dbContext.SaveChangesAsync();
    }
}
