using Microsoft.EntityFrameworkCore;
using PolyMod.AuthModule.Data;
using PolyMod.AuthModule.Models;
using PolyMod.AuthModule.Repositories;
using PolyMod.AuthModule.Services;
using PolyMod.Shared.Repositories;
using PolyMod.MetricsModule.OpenTelemetry;
using PolyMod.Shared.CachingConfiguration;
using PolyMod.Shared.Utils;

namespace PolyMod.AuthModule;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Database configuration
        services.AddDbContextPool<AuthDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("AuthDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));

        // Cache configuration
        services.Configure<CacheOptions>("Auth", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "Auth";
        });

        // Core auth services
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IHasher, Hasher>();

        // Generic repository with caching
        services.AddScoped<IGenericRepository<AuthUser>>(sp =>
            new GenericRepository<AuthUser>(sp.GetRequiredService<AuthDbContext>()));
        services.Decorate<IGenericRepository<AuthUser>, CachingRepositoryDecorator<AuthUser>>();

        // AutoMapper
        services.AddAutoMapper(_ => { }, typeof(AuthModule).Assembly);

        services.RegisterModuleForMetrics("AuthModule");

        return services;
    }
}
