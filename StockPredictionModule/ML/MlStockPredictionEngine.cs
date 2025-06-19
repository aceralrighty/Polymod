using Microsoft.ML;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.ML;

public class MlStockPredictionEngine
{
    private static readonly MLContext MlContext = new(seed: 0);
    private ITransformer? _model;
    private PredictionEngine<RawData, StockPrediction>? _predictionEngine;

    public Task<bool> IsModelTrainedAsync()
    {
        return Task.FromResult(_model != null && _predictionEngine != null);
    }

    public Task TrainModelAsync(List<RawData> rawData)
    {
        Console.WriteLine("Starting model training...");

        if (rawData == null || rawData.Count == 0)
        {
            throw new InvalidOperationException("No training data provided");
        }

        Console.WriteLine($"Training on {rawData.Count} historical records");

        var mlTrainingData = MlContext.Data.LoadFromEnumerable(rawData);

        var pipeline = MlContext.Transforms.CustomMapping<RawData, DateFeatures>(
                (input, output) =>
                {
                    if (!DateTime.TryParse(input.Date, out var date))
                    {
                        output.DayOfYear = 1;
                        output.DaysSinceEpoch = 0;
                        output.Year = 2000;
                        output.Month = 1;
                        output.DayOfWeek = 1;
                        return;
                    }

                    output.DayOfYear = date.DayOfYear;
                    output.DaysSinceEpoch = (float)(date - new DateTime(2000, 1, 1)).TotalDays;
                    output.Year = date.Year;
                    output.Month = date.Month;
                    output.DayOfWeek = (int)date.DayOfWeek;
                }, "DateFeatures")
            .Append(MlContext.Transforms.Text.FeaturizeText("SymbolFeatures", nameof(RawData.Symbol)))
            .Append(MlContext.Transforms.Concatenate("NumericFeatures",
                nameof(RawData.Open), nameof(RawData.High), nameof(RawData.Low),
                nameof(RawData.Volume), "DayOfYear", "DaysSinceEpoch", "Year", "Month", "DayOfWeek"))
            .Append(MlContext.Transforms.Concatenate("Features", "NumericFeatures", "SymbolFeatures"))
            .Append(MlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(RawData.Close),
                featureColumnName: "Features"));

        _model = pipeline.Fit(mlTrainingData);
        _predictionEngine = MlContext.Model.CreatePredictionEngine<RawData, StockPrediction>(_model);

        Console.WriteLine("Model training completed successfully");
        return Task.CompletedTask;
    }

    public Task<StockPrediction> GeneratePredictAsync(List<RawData> rawData, string symbol)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol cannot be null or empty", nameof(symbol));

        if (_predictionEngine == null)
            throw new InvalidOperationException("Model must be trained before generating predictions");

        var latestStock = rawData
            .Where(s => s.Symbol == symbol)
            .OrderByDescending(s => DateTime.Parse(s.Date))
            .FirstOrDefault();

        if (latestStock == null)
            throw new InvalidOperationException($"No historical data found for symbol: {symbol}");

        var input = new RawData
        {
            Date = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd"),
            Open = latestStock.Close,
            High = latestStock.Close * 1.05f,
            Low = latestStock.Close * 0.95f,
            Close = 0, // to be predicted
            Volume = latestStock.Volume,
            Symbol = symbol
        };

        var prediction = _predictionEngine.Predict(input);

        // The prediction.Price will be automatically mapped from the "Score" column
        var result = new StockPrediction
        {
            Id = Guid.NewGuid(),
            BatchId = Guid.NewGuid(),
            Price = prediction.Price, // This should work now
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            DeletedAt = null
        };

        Console.WriteLine($"Generated prediction for {symbol}: ${result.Price:F2}");
        return Task.FromResult(result);
    }
}
