using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Services.Interface;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class RecommendationsModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<RecommendationDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "recommendations";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(RecommendationDbContext dbContext, CancellationToken cancellationToken)
    {
        var recommendationService = ServiceProvider.GetService<IRecommendationService>();

        var totalRecommendations = await dbContext.RecommendationOutputs.CountAsync(cancellationToken);
        var isMlReady = recommendationService?.TrainRecommendationModelAsync();

        return new Dictionary<string, object>
        {
            { "totalRecommendations", totalRecommendations },
            { "mlModelReady", isMlReady ?? throw new InvalidOperationException("Something went wrong, when checking the health of the recommendations") }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ ML Ready";
    }

    protected override string GetDescription()
    {
        return "Machine learning recommendation engine";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/recommendations/user/{id}", "/api/recommendations/trending"];
    }
}
