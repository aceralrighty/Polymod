using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.ML;
using TBD.RecommendationModule.ML.Interface;
using TBD.RecommendationModule.Models.Recommendations;
using TBD.RecommendationModule.Repositories;
using TBD.RecommendationModule.Repositories.Interfaces;
using TBD.RecommendationModule.Seed;
using TBD.RecommendationModule.Services;
using TBD.RecommendationModule.Services.BackgroundProcesses;
using TBD.RecommendationModule.Services.Interface;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.Repositories;

namespace TBD.RecommendationModule;

public static class RecommendationModule
{
    public static IServiceCollection AddRecommendationModule(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RecommendationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("RecDb")));

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
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();

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
