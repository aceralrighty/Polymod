using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class AuthModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<AuthDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "auth";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(AuthDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var userCount = await dbContext.AuthUsers.CountAsync(cancellationToken);
        var recentLogins = await dbContext.AuthUsers.Where(u => u.LastLogin != null).CountAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            { "userCount", userCount }, { "recentLogins", recentLogins }, { "databaseConnected", true }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ Operational";
    }

    protected override string GetDescription()
    {
        return "JWT Authentication with user management";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/auth/login", "/api/auth/register", "/api/auth/refresh"];
    }
}
