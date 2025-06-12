using TBD.RecommendationModule.Models;
using TBD.Shared.Repositories;

namespace TBD.RecommendationModule.Repositories;

public interface IRecommendationRepository : IGenericRepository<UserRecommendation>
{
    new Task AddAsync(UserRecommendation userRecommendation);
    Task<IEnumerable<UserRecommendation>> GetByUserIdAsync(Guid userId);
    Task<UserRecommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId);
    Task SaveChangesAsync();

    Task<IEnumerable<UserRecommendation>> GetAllWithRatingsAsync();
    Task<IEnumerable<Guid>> GetMostPopularServicesAsync(int count);
    Task AddRatingAsync(Guid userId, Guid serviceId, float rating);
}
