using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Repositories;
using TBD.ServiceModule.Services;
using TBD.Shared.Utils;

namespace TBD.ServiceModule;

public static class ServiceModule
{
    public static IServiceCollection AddServiceModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ServiceDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("ServiceDb")));
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IServicesService, ServicesService>();
        services.AddAutoMapper(typeof(ServiceMapping).Assembly);
        return services;
    }
}
