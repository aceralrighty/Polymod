using TBD.RecommendationModule.Models.Recommendations;

namespace TBD.RecommendationModule.Repositories.Interfaces;

public interface IRecommendationOutputRepository
{
    Task<IEnumerable<RecommendationOutput>> GetLatestRecommendationsForUserAsync(Guid userId, int count = 10);
    Task<IEnumerable<RecommendationOutput>> GetRecommendationsByBatchAsync(Guid batchId);
    Task SaveRecommendationBatchAsync(IEnumerable<RecommendationOutput> recommendations);
    Task MarkAsViewedAsync(Guid userId, Guid serviceId);
    Task MarkAsClickedAsync(Guid userId, Guid serviceId);
    Task<RecommendationAnalytics> GetRecommendationAnalyticsAsync(Guid userId);
    Task<IEnumerable<RecommendationOutput>> GetRecommendationHistoryAsync(Guid userId, DateTime? since = null);
}
