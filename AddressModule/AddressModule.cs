using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Repositories;
using TBD.AddressModule.Services;
using TBD.MetricsModule.Services;

namespace TBD.AddressModule;

public static class AddressModule
{
    public static IServiceCollection AddAddressService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AddressDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("AddressDb")));
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IUserAddressService, UserAddressService>();
        services.AddAutoMapper(typeof(AddressModule).Assembly);
        return services;
    }

}
