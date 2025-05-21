// UserModule.cs

using Microsoft.EntityFrameworkCore;
using TBD.UserModule.Data;
using TBD.UserModule.Repositories;
using TBD.UserModule.Services;

namespace TBD.UserModule;

public static class UserModule
{
    public static IServiceCollection AddUserService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UserDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("UserDatabase")));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserService, UserService>();
        services.AddAutoMapper(typeof(UserModule).Assembly);
        return services;
    }

}