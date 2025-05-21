using TBD.AddressService;
using TBD.Data.Seeding;
using TBD.UserModule;

namespace TBD;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Modularized service registrations
        builder.Services.AddUserService(builder.Configuration);     // Register UserModule services
        builder.Services.AddAddressService(builder.Configuration); // Register AddressModule services

        // Shared components and features
        builder.Services.AddAuthorization();
        builder.Services.AddControllers();
        builder.Services.AddOpenApi();

        var app = builder.Build();

        // Seed data if enabled in configuration
        if (builder.Configuration.GetValue("SeedData", false))
        {
            await DataSeeder.SeedAsync(app.Services);
        }

        // Configure Middleware and Endpoints
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        await app.RunAsync(); // Run the web application
    }
}