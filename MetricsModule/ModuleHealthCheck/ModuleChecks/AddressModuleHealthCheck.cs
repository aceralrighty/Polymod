using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Services;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.DBLevel;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class AddressModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : DatabaseModuleHealthCheck<AddressDbContext>(serviceProvider, logger)
{
    public override string ModuleName => "address";

    protected override async Task<Dictionary<string, object>> GetAdditionalHealthDataAsync(AddressDbContext dbContext, CancellationToken cancellationToken)
    {
        var addressCount = await dbContext.UserAddress.CountAsync(cancellationToken);
        var geocodingService = ServiceProvider.GetService<IUserAddressService>();

        return new Dictionary<string, object>
        {
            { "addressCount", addressCount },
            { "geocodingAvailable", geocodingService != null }
        };
    }

    protected override string GetHealthyStatus(Dictionary<string, object> additionalData)
    {
        return "✅ Geographic data loaded";
    }

    protected override string GetDescription()
    {
        return "Address and location management";
    }

    protected override string[] GetEndpoints()
    {
        return ["/api/address", "/api/address/search"];
    }
}
