using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories;
using TBD.RecommendationModule.Seed;
using TBD.RecommendationModule.Services;
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

        services.AddScoped<IRecommendationRepository, RecommendationRepository>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IMlRecommendationEngine, MlRecommendationEngine>();
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();

        services.AddScoped<IGenericRepository<UserRecommendation>>(sp =>
            new GenericRepository<UserRecommendation>(sp.GetRequiredService<RecommendationDbContext>()));
        services.Decorate<IGenericRepository<UserRecommendation>, CachingRepositoryDecorator<UserRecommendation>>();

        services.AddHostedService<ModelTrainingBackgroundService>();

        // Register RecommendationSeederAndTrainer here
        services.AddScoped<RecommendationSeederAndTrainer>();

        return services;
    }
}
