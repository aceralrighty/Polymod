using TBD.AddressService;
using TBD.Data.Seeding;
using TBD.ScheduleModule;
using TBD.UserModule;

namespace TBD;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddUserService(builder.Configuration);
        builder.Services.AddAddressService(builder.Configuration);
        builder.Services.AddScheduleModule(builder.Configuration);

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

        // app.UseHttpsRedirection(); // Disabled for testing
        app.UseAuthorization();
        app.MapControllers();

        app.MapGet("/test", () => "Hello, World!");

        Console.WriteLine("Starting application...");
        await app.RunAsync("http://0.0.0.0:5000"); // Explicit HTTP binding
    }
}