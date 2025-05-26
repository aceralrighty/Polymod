using TBD.AddressModule;
using TBD.Data.Seeding;
using TBD.ScheduleModule;
using TBD.ServiceModule;
using TBD.Shared.Utils;
using TBD.UserModule;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddServiceModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(ServiceMapping));
builder.Services.AddAutoMapper(typeof(UserAddressMapping));
builder.Services.AddAutoMapper(typeof(UserMapping));
builder.Services.AddAutoMapper(typeof(UserScheduleMapping));
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        Console.WriteLine("🌱 Starting database seeding...");

        // Seed in order with error handling
        await DataSeeder.ReseedForTestingAsync(app.Services);
        await Task.Delay(1000); // Give DB time to settle

        await ScheduleSeeder.ReseedForTestingAsync(app.Services);
        await Task.Delay(1000);

        await ServiceSeeder.ReseedForTestingAsync(app.Services);

        Console.WriteLine("✅ All seeding complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding failed: {ex.Message}");
        throw new NullReferenceException("Seeding failed", ex);
    }

    app.MapOpenApi();
}

app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Starting Server😁\n");
await app.RunAsync();