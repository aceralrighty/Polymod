using Microsoft.EntityFrameworkCore;
using TBD.AuthModule.Data;
using TBD.AuthModule.Repositories;
using TBD.AuthModule.Services;

namespace TBD.AuthModule;

public static class AuthModule
{
    public static IServiceCollection AddAuthModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options => options.UseSqlServer(
            configuration.GetConnectionString("AuthDb")));
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddAutoMapper(typeof(AuthModule).Assembly);
        return services;
    }
}
