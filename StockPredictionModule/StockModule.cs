using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.OpenTelemetry;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.EntityMappers;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.Load;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.ML.Interface;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Models.Stocks;
using TBD.StockPredictionModule.PipelineOrchestrator;
using TBD.StockPredictionModule.PipelineOrchestrator.Interface;
using TBD.StockPredictionModule.Repository;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule;

/// <summary>
/// Provides stock module-related services, configurations, and dependencies for the application.
/// </summary>
/// <remarks>
/// The StockModule class is designed to encapsulate all service registrations,
/// database context configurations, caching policies, and other dependencies
/// that are relevant to the stock prediction module of the application.
/// This module includes configurations for the database context, caching options, and
/// registrations of repositories, pipelines, data loaders, mappers, and machine learning components.
/// It also integrates with telemetry for monitoring purposes.
/// </remarks>
public static class StockModule
{
    public static IServiceCollection AddStockModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<StockDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TradingDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null // Default value: null
            )));

        services.Configure<CacheOptions>("Stock", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Stock";
        });

        services.AddScoped<IStockRepository, StockRepository>();
        services.AddScoped<IStockPredictionRepository, StockPredictionRepository>();
        services.AddScoped<IStockPredictionPipeline, StockPredictionPipeline>();
        services.AddScoped<LoadCsvData>();
        services.AddScoped<StockEntityMapper>();
        services.AddScoped<StockPredictionPipeline>();
        services.RegisterModuleForMetrics("StockModule");

        services.AddScoped<IMlStockPredictionEngine, MlStockPredictionEngine>();

        services.AddScoped<IGenericRepository<RawData>>(sp =>
            new GenericRepository<RawData>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<RawData>, CachingRepositoryDecorator<RawData>>();
        services.AddScoped<IGenericRepository<StockPrediction>>(sp =>
            new GenericRepository<StockPrediction>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<StockPrediction>, CachingRepositoryDecorator<StockPrediction>>();

        return services;
    }
}
