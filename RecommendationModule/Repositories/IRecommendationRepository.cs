using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Repositories;

public interface IRecommendationRepository
{
    Task AddAsync(Recommendation recommendation);
    Task<IEnumerable<Recommendation>> GetByUserIdAsync(Guid userId);
    Task<Recommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId);
    Task SaveChangesAsync();
}
