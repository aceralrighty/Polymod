using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Repositories;
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
        services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();
        services.AddScoped<IGenericRepository<Recommendation>>(sp =>
            new GenericRepository<Recommendation>(sp.GetRequiredService<RecommendationDbContext>()));
        services.Decorate<IGenericRepository<Recommendation>, CachingRepositoryDecorator<Recommendation>>();
        return services;
    }
}
