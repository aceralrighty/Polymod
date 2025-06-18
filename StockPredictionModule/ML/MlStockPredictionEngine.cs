using Microsoft.ML;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Repository;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule.ML;

public class MlStockPredictionEngine(IStockRepository repository, IStockPredictionRepository predictionRepository)
{
    private static readonly MLContext MlContext = new(seed: 0);
    private readonly ITransformer _model;

    private readonly IDataView data =
        MlContext.Data.LoadFromTextFile<StockPrediction>("Dataset/all_stocks_5yr.csv", hasHeader: true,
            separatorChar: ',');

    private readonly string _dataPath = Path.Combine(Environment.CurrentDirectory, "Dataset", "all_stocks_5yr.csv");
    private PredictionEngine<Stock, StockPrediction> _predictionEngine;


    public Task<bool> IsModelTrainedAsync()
    {
        return Task.FromResult(File.Exists(_dataPath));
    }

    public async Task TrainModelAsync()
    {
        var allData = await repository.GetAllAsync();

        var trainingData = MlContext.Data.LoadFromEnumerable(allData);
        var pipeline = MlContext.Transforms
            .Concatenate("Features", "Date", "Open", "High", "Low", "Close", "Volume", "Symbol")
            .Append(MlContext.Regression.Trainers.Sdca());

        var model = pipeline.Fit(trainingData);

        try
        {
            var predictionDirectory = Path.GetDirectoryName(_dataPath);
            if (!string.IsNullOrEmpty(predictionDirectory) && !Directory.Exists(predictionDirectory))
            {
                Directory.CreateDirectory(predictionDirectory);
            }
            else if (Directory.Exists(predictionDirectory))
            {
                Console.WriteLine($"DEBUG: Directory '{predictionDirectory}' already exists.");
            }
            else
            {
                Console.WriteLine($"DEBUG: predictionDirectory is null or empty. Cannot create directory.");
            }

            MlContext.Model.Save(model, trainingData.Schema, _dataPath);
            Console.WriteLine($"=============== Model saved to {_dataPath} ===============");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving model: {ex.Message}");
            throw;
        }

        _predictionEngine = MlContext.Model.CreatePredictionEngine<Stock, StockPrediction>(model);
    }
}
