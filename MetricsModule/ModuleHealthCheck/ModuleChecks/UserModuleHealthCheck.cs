using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using TBD.UserModule.Data;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class UserModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<UserDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "user";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(UserDbContext dbContext, CancellationToken cancellationToken)
    {
        var userCount = await dbContext.Users.CountAsync(cancellationToken);
        var activeUsers = await dbContext.Users.Where(u => true).CountAsync(cancellationToken);

        return new Dictionary<string, object>
        {
            { "totalUsers", userCount },
            { "activeUsers", activeUsers }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ Active";
    }

    protected override string GetDescription()
    {
        return "User profile management";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/user", "/api/user/{id}", "/api/user/profile"];
    }
}
