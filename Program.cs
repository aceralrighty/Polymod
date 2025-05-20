using Microsoft.EntityFrameworkCore;
using TBD.Data;
using TBD.Data.Seeding;
using TBD.Repository.Services.Base;
using TBD.Repository.Services.Schedule;
using TBD.Repository.Services.Stats;
using TBD.Repository.Services.User;
using TBD.Repository.Services.UserAddress;
using TBD.Services;
using TBD.Services.Stats;
using TBD.Services.User;
using TBD.Services.UserAddress;

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
        builder.Services.AddScoped<IScheduleService, ScheduleService>();
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