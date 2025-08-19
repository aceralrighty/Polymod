using Microsoft.EntityFrameworkCore;
using PolyMod.RecommendationModule.Data;
using PolyMod.RecommendationModule.ML;
using PolyMod.RecommendationModule.ML.Interface;
using PolyMod.RecommendationModule.Models.Recommendations;
using PolyMod.RecommendationModule.Repositories;
using PolyMod.RecommendationModule.Repositories.Interfaces;
using PolyMod.RecommendationModule.Seed;
using PolyMod.RecommendationModule.Services;
using PolyMod.RecommendationModule.Services.BackgroundProcesses;
using PolyMod.RecommendationModule.Services.Interface;
using PolyMod.Shared.Repositories;
using PolyMod.MetricsModule.OpenTelemetry;
using PolyMod.MetricsModule.Services;
using PolyMod.MetricsModule.Services.Interfaces;
using PolyMod.Shared.CachingConfiguration;

namespace PolyMod.RecommendationModule;

public static class RecommendationModule
{
    public static IServiceCollection AddRecommendationModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContextPool<RecommendationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("RecDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));

        services.Configure<CacheOptions>("Recommendation", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Recommendation";
        });

        // Register repositories
        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services
            .AddScoped<IRecommendationOutputRepository, RecommendationOutputRepository>();

        // Register services
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IMlRecommendationEngine, MlRecommendationEngine>();
        services.RegisterModuleForMetrics("RecommendationModule");

        // Register generic repositories with caching
        services.AddScoped<IGenericRepository<UserRecommendation>>(sp =>
            new GenericRepository<UserRecommendation>(sp.GetRequiredService<RecommendationDbContext>()));
        services.Decorate<IGenericRepository<UserRecommendation>, CachingRepositoryDecorator<UserRecommendation>>();

        services.AddScoped<IGenericRepository<RecommendationOutput>>(sp =>
            new GenericRepository<RecommendationOutput>(sp.GetRequiredService<RecommendationDbContext>()));
        services.Decorate<IGenericRepository<RecommendationOutput>, CachingRepositoryDecorator<RecommendationOutput>>();

        // Background services
        services.AddHostedService<ModelTrainingBackgroundService>();
        services.AddScoped<RecommendationSeederAndTrainer>();

        return services;
    }
}
