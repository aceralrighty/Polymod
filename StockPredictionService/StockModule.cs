using Microsoft.EntityFrameworkCore;
using PolyMod.StockPredictionService.Context;
using PolyMod.StockPredictionService.EntityMapper;
using PolyMod.StockPredictionService.Load;
using PolyMod.StockPredictionService.ML;
using PolyMod.StockPredictionService.ML.Interface;
using PolyMod.StockPredictionService.Models;
using PolyMod.StockPredictionService.Models.Stocks;
using PolyMod.StockPredictionService.PipelineOrchestrator;
using PolyMod.StockPredictionService.PipelineOrchestrator.Interface;
using PolyMod.StockPredictionService.Repository;
using PolyMod.StockPredictionService.Repository.Interfaces;
using PolyMod.StockPredictionService.Services;
using PolyMod.StockPredictionService.Services.Interfaces;
using PolyMod.StockPredictionService.Shared.CachingConfiguration;
using PolyMod.StockPredictionService.Shared.Repositories;

namespace PolyMod.StockPredictionService;

public static class StockModule
{
    public static IServiceCollection AddStockModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<StockDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TradingDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));

        services.Configure<CacheOptions>("Stock", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Stock";
        });

        // Register only the factory - the IMetricsService will be registered in Program.cs using the factory
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();

        // Register repositories and services
        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockPredictionRepository, StockPredictionRepository>();
        services.AddScoped<IStockPredictionPipeline, StockPredictionPipeline>();
        services.AddScoped<LoadCsvData>();
        services.AddScoped<StockEntityMapper>();
        services.AddScoped<StockPredictionPipeline>();
        services.AddScoped<IMlStockPredictionEngine, MlStockPredictionEngine>();

        // Register generic repositories with caching
        services.AddScoped<IGenericRepository<RawData>>(sp =>
            new GenericRepository<RawData>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<RawData>, CachingRepositoryDecorator<RawData>>();

        services.AddScoped<IGenericRepository<StockPrediction>>(sp =>
            new GenericRepository<StockPrediction>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<StockPrediction>, CachingRepositoryDecorator<StockPrediction>>();

        return services;
    }
}
