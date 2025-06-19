using TBD.MetricsModule.Services;
using TBD.RecommendationModule.ML.Interface;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.RecommendationModule.Services.Interface;
using TBD.ServiceModule.Models;
using TBD.ServiceModule.Repositories;

namespace TBD.RecommendationModule.Services;

public class RecommendationService(
    IRecommendationRepository recommendationRepository,
    IMetricsServiceFactory serviceFactory,
    IServiceRepository service, IMlRecommendationEngine mlEngine) : IRecommendationService
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

    public async Task<IEnumerable<Service>> GetMlRecommendationsAsync(Guid userId, int count = 10)
    {
        _metricsService.IncrementCounter("rec.get_ml_recommendations");

        var recommendedServiceIds = await mlEngine.GenerateRecommendationsAsync(userId, count);
        return await service.GetByIdsAsync(recommendedServiceIds);
    }

    public async Task RateServiceAsync(Guid userId, Guid serviceId, float rating)
    {
        if (rating is < 1f or > 5f)
            throw new ArgumentException("Rating must be between 1 and 5");

        _metricsService.IncrementCounter("rec.rate_service");
        await recommendationRepository.AddRatingAsync(userId, serviceId, rating);
    }

    public async Task<float> PredictRatingAsync(Guid userId, Guid serviceId)
    {
        _metricsService.IncrementCounter("rec.predict_rating");
        return await mlEngine.PredictRatingAsync(userId, serviceId);
    }

    public async Task TrainRecommendationModelAsync()
    {
        _metricsService.IncrementCounter("rec.train_model");
        await mlEngine.TrainModelAsync();
    }
}
