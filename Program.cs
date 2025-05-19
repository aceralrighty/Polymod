using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Interfaces.Services;
using TBD.Repository;
using TBD.Repository.Base;
using TBD.Repository.Stats;
using TBD.Repository.User;
using TBD.Services;

namespace TBD;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Database context registration
        builder.Services.AddDbContextPool<GenericDatabaseContext>(u =>
            u.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        // Register repositories
        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IStatsRepository, StatsRepository>();
        builder.Services.AddScoped<IUserAddressService, UserAddressService>();

        // Add any other services you might need
        // builder.Services.AddScoped<IYourService, YourService>();

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Add API Controllers if using them
        builder.Services.AddControllers();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();

        app.UseAuthorization();
        
        // Map controllers if using them
        app.MapControllers();

        app.Run();
    }
}