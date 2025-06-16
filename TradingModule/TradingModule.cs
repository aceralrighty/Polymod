using Microsoft.EntityFrameworkCore;
using TBD.TradingModule.Core.Entities.Interfaces;
using TBD.TradingModule.Infrastructure.MarketData;
using TBD.TradingModule.ML;
using TBD.TradingModule.Orchestration;
using TBD.TradingModule.Preprocessing;

namespace TBD.TradingModule;

public static class TradingModule
{
    public static IServiceCollection AddTradingModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TradingDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TradingDb")));

        // Repository Pattern
        services.AddScoped<ITradingRepository, TradingRepository>();

        // Core Services
        services.AddScoped<MarketDataFetcher>();
        services.AddScoped<FeatureEngineeringService>();
        services.AddScoped<StockPredictionEngine>();

        // Main Orchestrator
        services.AddScoped<TrainingOrchestrator>();

        // HttpClient for API calls
        services.AddHttpClient();

        return services;
    }
}
