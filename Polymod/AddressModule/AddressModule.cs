using Microsoft.EntityFrameworkCore;
using PolyMod.AddressModule.Data;
using PolyMod.AddressModule.Models;
using PolyMod.AddressModule.Repositories;
using PolyMod.AddressModule.Services;
using PolyMod.Shared.Repositories;
using PolyMod.Shared.CachingConfiguration;

namespace PolyMod.AddressModule;

public static class AddressModule
{
    public static IServiceCollection AddAddressService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContextPool<AddressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AddressDb"), b => b.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            )));
        services.Configure<CacheOptions>("Address",
            options =>
            {
                options.DefaultCacheDuration = TimeSpan.FromMinutes(10);
                options.GetByIdCacheDuration = TimeSpan.FromMinutes(15);
                options.GetAllCacheDuration = TimeSpan.FromMinutes(5);
                options.EnableCaching = true;
                options.CacheKeyPrefix = "Address";
            });
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IUserAddressService, UserAddressService>();
        services.AddScoped<IGenericRepository<UserAddress>>(serviceProvider =>
            new GenericRepository<UserAddress>(serviceProvider.GetRequiredService<AddressDbContext>()));
        services.Decorate<IGenericRepository<UserAddress>, CachingRepositoryDecorator<UserAddress>>();
        services.AddAutoMapper(_ =>
        {
        }, typeof(AddressModule).Assembly);
        return services;
    }
}
