using TBD.ServiceModule.Models;

namespace TBD.RecommendationModule.Services;

public interface IRecommendationService
{
    Task<IEnumerable<Service>> GetRecommendationsForUserAsync(Guid userId);
    Task RecordRecommendationAsync(Guid userId, Guid serviceId);
    Task IncrementClickAsync(Guid userId, Guid serviceId);
}
