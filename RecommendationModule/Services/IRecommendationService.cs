using TBD.ServiceModule.Models;

namespace TBD.RecommendationModule.Services;

public interface IRecommendationService
{
    Task<IEnumerable<Service>> GetRecommendationsForUserAsync(Guid userId);
    Task RecordRecommendationAsync(Guid userId, Guid serviceId);
    Task IncrementClickAsync(Guid userId, Guid serviceId);

    Task<IEnumerable<Service>> GetMlRecommendationsAsync(Guid userId, int count = 10);
    Task RateServiceAsync(Guid userId, Guid serviceId, float rating);
    Task<float> PredictRatingAsync(Guid userId, Guid serviceId);
    Task TrainRecommendationModelAsync();
}
