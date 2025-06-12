using Microsoft.ML;
using Microsoft.ML.Trainers;
using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Repositories;

public class MlRecommendationEngine(IRecommendationRepository _repository) : IMlRecommendationEngine
{
    private readonly MLContext _mlContext = new(seed: 0);
    private ITransformer? _model;
    private readonly string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "RecommendationModel.zip");
    private PredictionEngine<ServiceRating, ServiceRatingPrediction>? _predictionEngine;

    public Task<bool> IsModelTrainedAsync()
    {
        return Task.FromResult(File.Exists(_modelPath) && _model != null);
    }

    public async Task TrainModelAsync()
    {
        Console.WriteLine("=============== Training ML.NET Recommendation Model ===============");

        // Get all user-service interactions with ratings
        var allRecommendations = await _repository.GetAllWithRatingsAsync();

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

        // Define the ML pipeline
        var estimator = _mlContext.Transforms.Conversion
            .MapValueToKey(outputColumnName: "userIdEncoded", inputColumnName: nameof(ServiceRating.UserId))
            .Append(_mlContext.Transforms.Conversion
                .MapValueToKey(outputColumnName: "serviceIdEncoded", inputColumnName: nameof(ServiceRating.ServiceId)));

        // Configure Matrix Factorization trainer
        var options = new MatrixFactorizationTrainer.Options
        {
            MatrixColumnIndexColumnName = "userIdEncoded",
            MatrixRowIndexColumnName = "serviceIdEncoded",
            LabelColumnName = "Label",
            NumberOfIterations = 20,
            ApproximationRank = 32, // Smaller rank for smaller datasets
            LearningRate = 0.1
        };

        var trainer = estimator.Append(_mlContext.Recommendation().Trainers.MatrixFactorization(options));

        // Train the model
        _model = trainer.Fit(dataView);

        // Create prediction engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);

        // Save the model
        _mlContext.Model.Save(_model, dataView.Schema, _modelPath);

        Console.WriteLine("=============== Model Training Complete ===============");
    }

    public Task<float> PredictRatingAsync(Guid userId, Guid serviceId)
    {
        if (_predictionEngine == null)
        {
            if (File.Exists(_modelPath))
            {
                // Load existing model
                _model = _mlContext.Model.Load(_modelPath, out _);
                _predictionEngine =
                    _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
            }
            else
            {
                // No model available, return default rating
                return Task.FromResult(2.5f);
            }
        }

        var testInput = new ServiceRating { UserId = HashGuid(userId), ServiceId = HashGuid(serviceId) };

        var prediction = _predictionEngine.Predict(testInput);
        return Task.FromResult(Math.Max(1f, Math.Min(5f, prediction.Score))); // Clamp between 1-5
    }

    public async Task<IEnumerable<Guid>> GenerateRecommendationsAsync(Guid userId, int maxResults = 10)
    {
        if (_predictionEngine == null && !await LoadModelAsync())
        {
            // Fallback to popularity-based recommendations
            return await GetPopularityBasedRecommendationsAsync(userId, maxResults);
        }

        // Get all services the user hasn't interacted with
        var userInteractions = await _repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        // For demo purposes, get all possible service IDs
        // In a real app, you'd get this from your service repository
        var allServiceIds = await GetAllServiceIdsAsync();
        var candidateServices = allServiceIds.Where(id => !interactedServiceIds.Contains(id)).ToList();

        // Predict ratings for all candidate services
        var predictions = new List<(Guid ServiceId, float PredictedRating)>();

        foreach (var serviceId in candidateServices)
        {
            var rating = await PredictRatingAsync(userId, serviceId);
            predictions.Add((serviceId, rating));
        }

        // Return top-rated services
        return predictions
            .Where(p => p.PredictedRating >= 3.5f) // Only recommend highly-rated services
            .OrderByDescending(p => p.PredictedRating)
            .Take(maxResults)
            .Select(p => p.ServiceId);
    }

    private Task<bool> LoadModelAsync()
    {
        if (!File.Exists(_modelPath)) return Task.FromResult(false);

        try
        {
            _model = _mlContext.Model.Load(_modelPath, out _);
            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private async Task<IEnumerable<Guid>> GetPopularityBasedRecommendationsAsync(Guid userId, int maxResults)
    {
        // Fallback: return most popular services this user hasn't tried
        var userInteractions = await _repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        var popularServices = await _repository.GetMostPopularServicesAsync(maxResults * 2);

        return popularServices
            .Where(serviceId => !interactedServiceIds.Contains(serviceId))
            .Take(maxResults);
    }

    private async Task<IEnumerable<Guid>> GetAllServiceIdsAsync()
    {
        // This would typically come from your service repository
        // For now, return services from existing recommendations
        var recommendations = await _repository.GetAllAsync();
        return recommendations.Select(r => r.ServiceId).Distinct();
    }

    // Helper method to convert Guid to float for ML.NET
    private float HashGuid(Guid guid)
    {
        return Math.Abs(guid.GetHashCode()) % 100000; // Simple hash to float conversion
    }

    public void Dispose()
    {
        _predictionEngine?.Dispose();
    }
}
