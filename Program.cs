using TBD.AddressModule;
using TBD.AuthModule;
using TBD.AuthModule.Seed;
using TBD.MetricsModule;
using TBD.ScheduleModule;
using TBD.ScheduleModule.Seed;
using TBD.ServiceModule;
using TBD.ServiceModule.Seed;
using TBD.Shared.Utils;
using TBD.UserModule;
using TBD.UserModule.Seed;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddMetricsModule();
builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddServiceModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(ServiceMapping));
builder.Services.AddAutoMapper(typeof(UserAddressMapping));
builder.Services.AddAutoMapper(typeof(UserMapping));
builder.Services.AddAutoMapper(typeof(UserScheduleMapping));
builder.Services.AddAutoMapper(typeof(AuthUserMapping));
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
        await Task.Delay(1000);
        await AuthSeeder.ReseedSeedAsync(app.Services);

        Console.WriteLine("✅ All seeding complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding failed: {ex.Message}");
        throw new NullReferenceException("Seeding failed", ex);
    }

    app.MapOpenApi();
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("Starting Server😁\n");
await app.RunAsync();
