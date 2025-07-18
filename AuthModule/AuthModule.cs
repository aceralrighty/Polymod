using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.AuthModule.Repositories;
using TBD.AuthModule.Services;
using TBD.MetricsModule.OpenTelemetry;
using TBD.MetricsModule.Services.Interfaces;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.Repositories;
using TBD.Shared.Utils;

namespace TBD.AuthModule;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        // Database configuration
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("AuthDb")));

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
        services.AddAutoMapper(typeof(AuthModule).Assembly);

        services.RegisterModuleForMetrics("AuthModule");

        return services;
    }
}
