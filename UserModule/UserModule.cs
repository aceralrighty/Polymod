using Microsoft.EntityFrameworkCore;
using TBD.API.Interfaces;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;

namespace TBD.UserModule;

public static class UserModule
{
    public static IServiceCollection AddUserService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("UserDb")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IHasher, Hasher>();
        services.AddAutoMapper(typeof(UserMapping).Assembly);

        return services;
    }
}
