using TBD.RecommendationModule.Models.Recommendations;
using TBD.Shared.Repositories;
using TBD.Shared.Repositories.Configuration;

namespace TBD.RecommendationModule.Repositories.Interfaces;

public interface IRecommendationRepository : IGenericRepository<UserRecommendation>
{
    new Task AddAsync(UserRecommendation userRecommendation);
    Task<IEnumerable<UserRecommendation>> GetByUserIdAsync(Guid userId);
    Task<UserRecommendation?> GetLatestByUserAndServiceAsync(Guid userId, Guid serviceId);
    Task SaveChangesAsync();

    // Original method (now optimized)
    Task<IEnumerable<UserRecommendation>> GetAllWithRatingsAsync();

    // New optimized methods for different scenarios
    Task<IEnumerable<UserRecommendation>> GetAllWithRatingsChunkedAsync(int chunkSize = 10000);
    IAsyncEnumerable<UserRecommendation> GetAllWithRatingsStreamingAsync(int bufferSize = 5000);
    Task<IEnumerable<UserRecommendation>> GetAllWithRatingsConfigurableAsync(QueryOptions? options = null);

    Task<IEnumerable<Guid>> GetMostPopularServicesAsync(int count);
    Task AddRatingAsync(Guid userId, Guid serviceId, float rating);
}
