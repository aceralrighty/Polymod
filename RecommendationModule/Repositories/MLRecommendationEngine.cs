using Microsoft.ML;
using Microsoft.ML.Trainers;
using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Repositories;

public class MlRecommendationEngine(IRecommendationRepository repository) : IMlRecommendationEngine
{
    private readonly MLContext _mlContext = new(seed: 0);
    private ITransformer? _model;
    private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "Data", "RecommendationModel.zip");
    private PredictionEngine<ServiceRating, ServiceRatingPrediction>? _predictionEngine;

    public Task<bool> IsModelTrainedAsync()
    {
        return Task.FromResult(File.Exists(_modelPath) && _model != null);
    }

    public async Task TrainModelAsync()
    {
        Console.WriteLine("=============== Training ML.NET Recommendation Model ===============");

        // Get all user-service interactions with ratings
        var allRecommendations = await repository.GetAllWithRatingsAsync();

        if (!allRecommendations.Any())
        {
            Console.WriteLine("No training data available. Skipping model training.");
            return;
        }

        // Convert to ML.NET format
        var trainingData = allRecommendations.Select(r => new ServiceRating
        {
            UserId = HashGuid(r.UserId), // Convert Guid to float
            ServiceId = HashGuid(r.ServiceId),
            Label = r.Rating
        }).ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Define data preparation and training pipeline
        var pipeline = _mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "UserId", outputColumnName: "UserIdEncoded")
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "ServiceId", outputColumnName: "ServiceIdEncoded"))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = "UserIdEncoded",
                    MatrixRowIndexColumnName = "ServiceIdEncoded",
                    LabelColumnName = "Label",
                    NumberOfIterations = 20,
                    ApproximationRank = 100 // Adjust as needed
                }));

        // Train the model
        _model = pipeline.Fit(dataView);

        // Save the trained model
        try
        {
            // Ensure the directory exists before saving the model
            var modelDirectory = Path.GetDirectoryName(_modelPath);

            Console.WriteLine($"DEBUG: Model Path: {_modelPath}");
            Console.WriteLine($"DEBUG: Model Directory: {modelDirectory}");

            if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
            {
                Console.WriteLine($"DEBUG: Directory '{modelDirectory}' does not exist. Attempting to create...");
                Directory.CreateDirectory(modelDirectory);
                Console.WriteLine($"DEBUG: Directory exists after creation attempt: {Directory.Exists(modelDirectory)}");
            }
            else if (Directory.Exists(modelDirectory))
            {
                Console.WriteLine($"DEBUG: Directory '{modelDirectory}' already exists.");
            }
            else
            {
                Console.WriteLine($"DEBUG: modelDirectory is null or empty. Cannot create directory.");
            }

            // This is the line where the error consistently occurs
            _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
            Console.WriteLine($"=============== Model saved to {_modelPath} ===============");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving model: {ex.Message}");
            throw;
        }

        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
    }

    public async Task<IEnumerable<Guid>> GenerateRecommendationsAsync(Guid userId, int maxResults)
    {
        if (_model == null)
        {
            // Try to load model if it exists
            if (File.Exists(_modelPath))
            {
                await LoadModelAsync();
            }
            else
            {
                Console.WriteLine("Model not trained or loaded. Generating popularity-based recommendations as fallback.");
                return await GetPopularityBasedRecommendationsAsync(userId, maxResults);
            }
        }

        if (_predictionEngine == null)
        {
            Console.WriteLine("Prediction engine not initialized. Generating popularity-based recommendations as fallback.");
            return await GetPopularityBasedRecommendationsAsync(userId, maxResults);
        }

        var allServiceIds = (await GetAllServiceIdsAsync()).ToList();
        var userInteractions = await repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        var scores = new List<(Guid ServiceId, float Score)>();

        foreach (var serviceId in allServiceIds)
        {
            // Skip services the user has already interacted with
            if (interactedServiceIds.Contains(serviceId))
            {
                continue;
            }

            var prediction = _predictionEngine.Predict(new ServiceRating
            {
                UserId = HashGuid(userId),
                ServiceId = HashGuid(serviceId)
            });
            scores.Add((serviceId, prediction.Score));
        }

        return scores.OrderByDescending(s => s.Score)
            .Take(maxResults)
            .Select(s => s.ServiceId);
    }

    public async Task<float> PredictRatingAsync(Guid userId, Guid serviceId)
    {
        if (_model == null)
        {
            // Try to load model if it exists
            if (File.Exists(_modelPath))
            {
                await LoadModelAsync();
            }
            else
            {
                throw new InvalidOperationException("Model not trained or loaded. Cannot predict rating.");
            }
        }

        if (_predictionEngine == null)
        {
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
        }

        var prediction = _predictionEngine.Predict(new ServiceRating
        {
            UserId = HashGuid(userId),
            ServiceId = HashGuid(serviceId)
        });

        return prediction.Score;
    }

    private async Task<bool> LoadModelAsync()
    {
        if (!File.Exists(_modelPath))
        {
            Console.WriteLine($"Model file not found at {_modelPath}.");
            return false;
        }

        try
        {
            using (var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                _model = _mlContext.Model.Load(stream, out var modelInputSchema);
            }
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
            Console.WriteLine("ML.NET model loaded successfully.");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading ML.NET model: {ex.Message}");
            return false;
        }
    }

    private async Task<IEnumerable<Guid>> GetPopularityBasedRecommendationsAsync(Guid userId, int maxResults)
    {
        // Fallback: return most popular services this user hasn't tried
        var userInteractions = await repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        var popularServices = await repository.GetMostPopularServicesAsync(maxResults * 2);

        return popularServices
            .Where(serviceId => !interactedServiceIds.Contains(serviceId))
            .Take(maxResults);
    }

    private async Task<IEnumerable<Guid>> GetAllServiceIdsAsync()
    {
        // This would typically come from your service repository
        // For now, return services from existing recommendations
        var recommendations = await repository.GetAllAsync();
        return recommendations.Select(r => r.ServiceId).Distinct();
    }

    // Helper method to convert Guid to float for ML.NET
    private float HashGuid(Guid guid)
    {
        return Math.Abs(guid.GetHashCode()) % 100000; // Simple hash to float conversion
    }

    public void Dispose()
    {
        // Dispose any unmanaged resources if necessary
    }
}
