using Microsoft.EntityFrameworkCore;
using PolyMod.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using PolyMod.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using PolyMod.ScheduleModule.Data;

namespace PolyMod.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class ScheduleModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<ScheduleDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "schedule";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(ScheduleDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var scheduleCount = await dbContext.Schedules.CountAsync(cancellationToken);
        var upcomingSchedules = await dbContext.Schedules.Where(s => true).CountAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            { "totalSchedules", scheduleCount }, { "upcomingSchedules", upcomingSchedules }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ Scheduling active";
    }

    protected override string GetDescription()
    {
        return "User scheduling and availability";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/schedule", "/api/schedule/user/{id}", "/api/schedule/availability"];
    }
}
