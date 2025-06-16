using Microsoft.EntityFrameworkCore;
using TBD.Shared.CachingConfiguration;
using TBD.TradingModule.Infrastructure.MarketData;
using TBD.TradingModule.MarketData;
using TBD.TradingModule.Repository;

namespace TBD.TradingModule;

public static class TradingModule
{
    public static IServiceCollection AddTradingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TradingDb")));

        services.Configure<CacheOptions>("Trading", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Trading";
        });

        services.AddScoped<ITradingRepository, TradingRepository>();

        return services;
    }
}
