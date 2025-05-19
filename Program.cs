using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Data.Seeding;
using TBD.Interfaces.Services;
using TBD.Repository.Base;
using TBD.Repository.Stats;
using TBD.Repository.User;
using TBD.Repository.UserAddress;
using TBD.Services;

namespace TBD;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Service registration
        builder.Services.AddDbContextPool<GenericDatabaseContext>(u =>
            u.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

        builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        builder.Services.AddScoped<IUserRepository, UserRepository>();
        builder.Services.AddScoped<IStatsRepository, StatsRepository>();
        builder.Services.AddScoped<IUserAddressRepository, UserAddressService>();
        builder.Services.AddScoped<IUserAddressService, UserAddressService>();
        builder.Services.AddAutoMapper(typeof(Program));
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        if (builder.Configuration.GetValue("SeedData", false))
        {
            await DataSeeder.SeedAsync(app.Services);
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync(); // ✅ Corrected
    }
}