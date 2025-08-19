using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using TBD.ServiceModule.Data;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class ServiceModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<ServiceDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "service";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(ServiceDbContext dbContext, CancellationToken cancellationToken)
    {
        var serviceCount = await dbContext.Services.CountAsync(cancellationToken);
        var activeServices = await dbContext.Services.Where(s => true).CountAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            { "totalServices", serviceCount },
            { "activeServices", activeServices }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ Service catalog ready";
    }

    protected override string GetDescription()
    {
        return "Service management and catalog";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/service", "/api/service/{id}", "/api/service/categories"];
    }
}
