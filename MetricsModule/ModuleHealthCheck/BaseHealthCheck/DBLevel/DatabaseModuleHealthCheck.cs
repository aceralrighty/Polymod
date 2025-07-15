using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Model;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;

namespace TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;

public abstract class DatabaseModuleHealthCheck<TDbContext>(
    IServiceProvider serviceProvider,
    ILogger<BaseModuleHealthCheck> logger)
    : BaseModuleHealthCheck(serviceProvider, logger)
    where TDbContext : DbContext
{
    protected override async Task<ModuleHealthResult> PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        var dbContext = ServiceProvider.GetService<TDbContext>();

        if (dbContext == null)
        {
            return new ModuleHealthResult
            {
                Status = "❌ Service not available",
                Description = $"{ModuleName} database context not registered",
                IsHealthy = false,
                Endpoints = GetEndpoints()
            };
        }

        // Test database connectivity
        await dbContext.Database.CanConnectAsync(cancellationToken);

        // Get additional health data
        var additionalData = await GetAdditionalHealthDataAsync(dbContext, cancellationToken);

        return new ModuleHealthResult
        {
            Status = GetHealthyStatus(additionalData),
            Description = GetDescription(),
            IsHealthy = true,
            Endpoints = GetEndpoints(),
            AdditionalData = additionalData
        };
    }

    protected abstract Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(TDbContext dbContext, CancellationToken cancellationToken);
    protected abstract string GetHealthyStatus(Dictionary<string, object> additionalData);
    protected abstract string GetDescription();

}
