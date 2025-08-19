using Microsoft.EntityFrameworkCore;
using StockPredictionService.Context;
using StockPredictionService.CrossCutting.CachingConfiguration;
using StockPredictionService.CrossCutting.Repositories;
using StockPredictionService.EntityMapper;
using StockPredictionService.Load;
using StockPredictionService.ML;
using StockPredictionService.ML.Interface;
using StockPredictionService.Models;
using StockPredictionService.Models.Stocks;
using StockPredictionService.PipelineOrchestrator;
using StockPredictionService.PipelineOrchestrator.Interface;
using StockPredictionService.Repository;
using StockPredictionService.Repository.Interfaces;
using StockPredictionService.Services;
using StockPredictionService.Services.Interfaces;

namespace StockPredictionService;

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
