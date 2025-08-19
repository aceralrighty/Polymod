namespace TBD.RecommendationModule.ML.Interface;

public interface IMlRecommendationEngine
{
    Task<IEnumerable<Guid>> GenerateRecommendationsAsync(Guid userId, int maxResults = 10);
    Task<float> PredictRatingAsync(Guid userId, Guid serviceId);
    Task TrainModelAsync();
    Task<bool> IsModelTrainedAsync();
}
