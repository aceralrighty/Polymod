using Microsoft.ML;
using Microsoft.ML.Trainers;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.RecommendationModule.ML.Interface;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.RecommendationModule.Repositories.Interfaces;

namespace TBD.RecommendationModule.ML;

/// <summary>
/// The MlRecommendationEngine class provides functionality for generating
/// personalized recommendations, model training, and prediction of ratings.
/// Implements the IMlRecommendationEngine interface.
/// </summary>
/// <remarks>
/// This class uses dependency injection to include implementations of:
/// - IRecommendationRepository for interaction with recommendation-related data.
/// - IRecommendationOutputRepository for outputting recommendation data.
/// - IMetricsServiceFactory for tracking metrics.
/// </remarks>
/// <dependencies>
/// Requires instances of IRecommendationRepository, IRecommendationOutputRepository,
/// and IMetricsServiceFactory to be provided via dependency injection.
/// </dependencies>
/// <methods>
/// - IsModelTrainedAsync: Determines if the recommendation model is trained and available.
/// - TrainModelAsync: Trains the recommendation model using the provided data repository.
/// - GenerateRecommendationsAsync: Generates a list of recommendations for a specific user.
/// - PredictRatingAsync: Predicts the rating a user would give to a particular service.
/// </methods>
internal class MlRecommendationEngine(
    IRecommendationRepository repository,
    IRecommendationOutputRepository outputRepository,
    IMetricsServiceFactory serviceFactory) : IMlRecommendationEngine
{
    /// <summary>
    /// Represents an instance of the ML.NET machine learning context which is used as the
    /// primary entry point for machine learning operations within the MLRecommendationEngine class.
    /// It provides methods and utilities for data loading, transformations, training, testing,
    /// evaluation, and model management.
    /// </summary>
    private readonly MLContext _mlContext = new(seed: 0);
    private ITransformer? _model;
    private readonly string _modelPath = Path.Combine(AppContext.BaseDirectory, "Data", "RecommendationModel.zip");
    private PredictionEngine<ServiceRating, ServiceRatingPrediction>? _predictionEngine;
    private readonly IMetricsService _metricsService = serviceFactory.CreateMetricsService("Recommendation");

    public Task<bool> IsModelTrainedAsync()
    {
        _metricsService.IncrementCounter("rec.is_model_trained");
        return Task.FromResult(File.Exists(_modelPath) && _model != null);
    }

    public async Task TrainModelAsync()
    {
        _metricsService.IncrementCounter("rec.train_model");
        var allRecommendations = await repository.GetAllWithRatingsAsync();

        var userRecommendations = allRecommendations as UserRecommendation[] ?? allRecommendations.ToArray();
        if (userRecommendations.Length == 0)
        {
            _metricsService.IncrementCounter("rec.train_model_empty_data");
            return;
        }

        var trainingData = userRecommendations.Select(r => new ServiceRating
        {
            UserId = HashGuid(r.UserId), ServiceId = HashGuid(r.ServiceId), Label = r.Rating
        }).ToList();

        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        _metricsService.IncrementCounter("rec.prep_model_training_pipeline");
        var pipeline = _mlContext.Transforms.Conversion
            .MapValueToKey(inputColumnName: "UserId", outputColumnName: "UserIdEncoded")
            .Append(_mlContext.Transforms.Conversion.MapValueToKey(inputColumnName: "ServiceId",
                outputColumnName: "ServiceIdEncoded"))
            .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                new MatrixFactorizationTrainer.Options
                {
                    MatrixColumnIndexColumnName = "UserIdEncoded",
                    MatrixRowIndexColumnName = "ServiceIdEncoded",
                    LabelColumnName = "Label",
                    NumberOfIterations = 20,
                    ApproximationRank = 100,
                }));

        // Train the model
        _model = pipeline.Fit(dataView);

        // Save the trained model
        try
        {
            var modelDirectory = Path.GetDirectoryName(_modelPath);

            Console.WriteLine($"DEBUG: Model Path: {_modelPath}");
            Console.WriteLine($"DEBUG: Model Directory: {modelDirectory}");

            if (!string.IsNullOrEmpty(modelDirectory) && !Directory.Exists(modelDirectory))
            {
                Console.WriteLine($"DEBUG: Directory '{modelDirectory}' does not exist. Attempting to create...");
                Directory.CreateDirectory(modelDirectory);
                Console.WriteLine(
                    $"DEBUG: Directory exists after creation attempt: {Directory.Exists(modelDirectory)}");
            }
            else if (Directory.Exists(modelDirectory))
            {
                Console.WriteLine($"DEBUG: Directory '{modelDirectory}' already exists.");
            }
            else
            {
                Console.WriteLine($"DEBUG: modelDirectory is null or empty. Cannot create directory.");
            }

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
        var batchId = Guid.NewGuid();
        var context = GetCurrentContext();

        if (_model == null)
        {
            if (File.Exists(_modelPath))
            {
                await LoadModelAsync();
            }
            else
            {
                Console.WriteLine(
                    "Model not trained or loaded. Generating popularity-based recommendations as fallback.");
                var fallbackRecs = await GetPopularityBasedRecommendationsAsync(userId, maxResults);
                var generateRecommendationsAsync = fallbackRecs as Guid[] ?? fallbackRecs.ToArray();
                await SaveRecommendationOutputsAsync(userId, generateRecommendationsAsync, batchId, "Popularity",
                    context);
                return generateRecommendationsAsync;
            }
        }

        if (_predictionEngine == null)
        {
            Console.WriteLine(
                "Prediction engine not initialized. Generating popularity-based recommendations as fallback.");
            var fallbackRecs = await GetPopularityBasedRecommendationsAsync(userId, maxResults);
            var generateRecommendationsAsync = fallbackRecs as Guid[] ?? fallbackRecs.ToArray();
            await SaveRecommendationOutputsAsync(userId, generateRecommendationsAsync, batchId, "Popularity", context);
            return generateRecommendationsAsync;
        }

        var allServiceIds = (await GetAllServiceIdsAsync()).ToList();
        var userInteractions = await repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        var scores = (from serviceId in allServiceIds
            where !interactedServiceIds.Contains(serviceId)
            let prediction =
                _predictionEngine.Predict(new ServiceRating
                {
                    UserId = HashGuid(userId), ServiceId = HashGuid(serviceId)
                })
            select (serviceId, prediction.Score)).ToList();

        var recommendedServiceIds = scores.OrderByDescending(s => s.Score)
            .Take(maxResults)
            .Select(s => s.serviceId)
            .ToList();

        // Save the recommendation outputs to a database
        await SaveRecommendationOutputsAsync(userId, scores.Take(maxResults), batchId, "MatrixFactorization", context);

        return recommendedServiceIds;
    }

    public async Task<float> PredictRatingAsync(Guid userId, Guid serviceId)
    {
        if (_model == null)
        {
            if (File.Exists(_modelPath))
            {
                await LoadModelAsync();
            }
            else
            {
                throw new InvalidOperationException("Model not trained or loaded. Cannot predict rating.");
            }
        }

        _predictionEngine ??= _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);

        var prediction = _predictionEngine.Predict(new ServiceRating
        {
            UserId = HashGuid(userId), ServiceId = HashGuid(serviceId)
        });

        return prediction.Score;
    }

    private async Task SaveRecommendationOutputsAsync(
        Guid userId,
        IEnumerable<(Guid ServiceId, float Score)> recommendations,
        Guid batchId,
        string strategy,
        string context)
    {
        var outputs = recommendations.Select((rec, index) => new RecommendationOutput
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = rec.ServiceId,
            Score = rec.Score,
            Rank = index + 1,
            Strategy = strategy,
            Context = context,
            BatchId = batchId,
            GeneratedAt = DateTime.UtcNow
        }).ToList();

        await outputRepository.SaveRecommendationBatchAsync(outputs);

        _metricsService.IncrementCounter("rec.outputs_saved");
        Console.WriteLine($"Saved {outputs.Count} recommendation outputs for user {userId} (Batch: {batchId})");
    }

    // Overload for simple service IDs (for fallback methods)
    private async Task SaveRecommendationOutputsAsync(
        Guid userId,
        IEnumerable<Guid> serviceIds,
        Guid batchId,
        string strategy,
        string context)
    {
        var recommendations = serviceIds.Select((serviceId, _) => (serviceId, Score: 0f));
        await SaveRecommendationOutputsAsync(userId, recommendations, batchId, strategy, context);
    }

    private string GetCurrentContext()
    {
        var hour = DateTime.Now.Hour;
        var dayOfWeek = DateTime.Now.DayOfWeek;

        var timeOfDay = hour switch
        {
            >= 6 and < 12 => "Morning",
            >= 12 and < 17 => "Afternoon",
            >= 17 and < 22 => "Evening",
            _ => "Night"
        };

        var dayType = dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? "Weekend" : "Weekday";

        return $"{timeOfDay}_{dayType}";
    }

    private Task<bool> LoadModelAsync()
    {
        if (!File.Exists(_modelPath))
        {
            Console.WriteLine($"Model file not found at {_modelPath}.");
            return Task.FromResult(false);
        }

        try
        {
            using (var stream = new FileStream(_modelPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                _model = _mlContext.Model.Load(stream, out _);
            }

            _predictionEngine = _mlContext.Model.CreatePredictionEngine<ServiceRating, ServiceRatingPrediction>(_model);
            Console.WriteLine("ML.NET model loaded successfully.");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading ML.NET model: {ex.Message}");
            return Task.FromResult(false);
        }
    }

    private async Task<IEnumerable<Guid>> GetPopularityBasedRecommendationsAsync(Guid userId, int maxResults)
    {
        var userInteractions = await repository.GetByUserIdAsync(userId);
        var interactedServiceIds = userInteractions.Select(r => r.ServiceId).ToHashSet();

        var popularServices = await repository.GetMostPopularServicesAsync(maxResults * 2);

        return popularServices
            .Where(serviceId => !interactedServiceIds.Contains(serviceId))
            .Take(maxResults);
    }

    private async Task<IEnumerable<Guid>> GetAllServiceIdsAsync()
    {
        var recommendations = await repository.GetAllAsync();
        return recommendations.Select(r => r.ServiceId).Distinct();
    }

    private float HashGuid(Guid guid)
    {
        return Math.Abs(guid.GetHashCode()) % 100000;
    }
}
