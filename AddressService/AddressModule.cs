using Microsoft.EntityFrameworkCore;
using TBD.AddressService.Data;
using TBD.AddressService.Repositories;
using TBD.AddressService.Services;

namespace TBD.AddressService;

public static class AddressModule
{
    public static IServiceCollection AddAddressService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AddressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AddressDatabase")));
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IUserAddressService, UserAddressService>();
        services.AddAutoMapper(typeof(AddressModule).Assembly);
        return services;
    }

}