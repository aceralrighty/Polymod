using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;

namespace TBD.RecommendationModule.Services;

public class RecommendationService(
    IRecommendationRepository recommendationRepository,
    IMetricsServiceFactory serviceFactory,
    IServiceRepository service) : IRecommendationService
{
    private readonly IMetricsService _metricsService = serviceFactory.CreateMetricsService("Recommendation");

    public async Task<IEnumerable<Service>> GetRecommendationsForUserAsync(Guid userId)
    {
        _metricsService.IncrementCounter("rec.get_recommendations_for_user.");
        var recs = await recommendationRepository.GetByUserIdAsync(userId);
        var serviceIds = recs.Select(r => r.ServiceId).Distinct();
        return await service.GetByIdsAsync(serviceIds);
    }

    public async Task RecordRecommendationAsync(Guid userId, Guid serviceId)
    {
        var existing = await recommendationRepository.GetLatestByUserAndServiceAsync(userId, serviceId);
        if (existing != null)
        {
            return;
        }

        var recommendation = new UserRecommendation()
        {
            UserId = userId, ServiceId = serviceId, RecommendedAt = DateTime.UtcNow
        };
        _metricsService.IncrementCounter("rec.record_recommendation.");
        await recommendationRepository.AddAsync(recommendation);
        await recommendationRepository.SaveChangesAsync();
    }

    public async Task IncrementClickAsync(Guid userId, Guid serviceId)
    {
        var rec = await recommendationRepository.GetLatestByUserAndServiceAsync(userId, serviceId);
        if (rec != null)
        {
            rec.ClickCount++;
            _metricsService.IncrementCounter("rec.increment_click.");
            await recommendationRepository.SaveChangesAsync();
        }
    }
}
