using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.AddressModule.Repositories;
using TBD.AddressModule.Services;
using TBD.Shared.Repositories;

namespace TBD.AddressModule;

public static class AddressModule
{
    public static IServiceCollection AddAddressService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AddressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AddressDb")));
        services.Configure<CacheOptions>("Address", options =>
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
        services.AddAutoMapper(typeof(AddressModule).Assembly);
        return services;
    }
}
