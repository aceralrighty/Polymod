using Microsoft.EntityFrameworkCore;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.Repositories;
using TBD.StockPredictionModule.Context;
using TBD.StockPredictionModule.ML;
using TBD.StockPredictionModule.Models;
using TBD.StockPredictionModule.Repository;
using TBD.StockPredictionModule.Repository.Interfaces;

namespace TBD.StockPredictionModule;

public static class StockModule
{
    public static IServiceCollection AddStockModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<StockDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TradingDb")));

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

        services.AddScoped<MlStockPredictionEngine>();

        services.AddScoped<IGenericRepository<RawData>>(sp =>
            new GenericRepository<RawData>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<RawData>, CachingRepositoryDecorator<RawData>>();
        services.AddScoped<IGenericRepository<StockPrediction>>(sp =>
            new GenericRepository<StockPrediction>(sp.GetRequiredService<StockDbContext>()));
        services.Decorate<IGenericRepository<StockPrediction>, CachingRepositoryDecorator<StockPrediction>>();

        return services;
    }
}
