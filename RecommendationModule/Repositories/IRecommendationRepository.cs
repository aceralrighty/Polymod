using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Repositories;

public interface IRecommendationRepository
{
    Task AddAsync(UserRecommendation userRecommendation);
    Task<IEnumerable<UserRecommendation>> GetByUserIdAsync(Guid userId);
    Task<UserRecommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId);
    Task SaveChangesAsync();
}
