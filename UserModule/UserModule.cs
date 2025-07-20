using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.OpenTelemetry;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.Shared.CachingConfiguration;
using TBD.Shared.EntityMappers;
using TBD.Shared.Events.Interfaces;
using TBD.Shared.Repositories;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;
using TBD.UserModule.PubEvents;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;

namespace TBD.UserModule;

public static class UserModule
{
    public static IServiceCollection AddUserService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("UserDb")));

        // Configure caching specifically for the User module
        services.Configure<CacheOptions>("User", options =>
        {
            options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
            options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
            options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
            options.EnableCaching = true;
            options.CacheKeyPrefix = "User";
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IGenericRepository<User>>(sp =>
            new GenericRepository<User>(sp.GetRequiredService<UserDbContext>()));

        // Just decorate - no need for manual registration
        services.Decorate<IGenericRepository<User>, CachingRepositoryDecorator<User>>();
        services.Configure<KestrelServerOptions>(options =>
        {
            options.AllowSynchronousIO = true;
        });

        services.AddLogging();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IUserReadService, UserReadService>();
        services.AddScoped<IHasher, Hasher>();
        services.AddAutoMapper(typeof(UserMapping).Assembly);
        services.RegisterModuleForMetrics("UserModule");

        // Register this module for metrics
        services.RegisterModuleForMetrics("UserModule");

        return services;
    }
}
