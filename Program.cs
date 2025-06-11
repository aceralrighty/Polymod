using TBD.AddressModule;
using TBD.AuthModule;
using TBD.AuthModule.Seed;
using TBD.MetricsModule;
using TBD.RecommendationModule;
using TBD.RecommendationModule.Seed;
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
builder.Services.AddRecommendationModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
builder.Services.AddAutoMapper(typeof(ServiceMapping));
builder.Services.AddAutoMapper(typeof(UserAddressMapping));
builder.Services.AddAutoMapper(typeof(UserMapping));
builder.Services.AddAutoMapper(typeof(UserScheduleMapping));
builder.Services.AddAutoMapper(typeof(AuthUserMapping));
builder.Services.AddMemoryCache();
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    try
    {
        Console.WriteLine("🌱 Starting database seeding...");

        // Seed users first and capture the result
        Console.WriteLine("👥 Seeding users...");
        var seededUsers = await DataSeeder.ReseedForTestingAsync(app.Services);
        Console.WriteLine($"✅ User seeding complete - {seededUsers.Count} users created");
        await Task.Delay(3000); // Reduced delay

        // Seed schedules
        Console.WriteLine("📅 Seeding schedules...");
        await ScheduleSeeder.ReseedForTestingAsync(app.Services);
        Console.WriteLine("✅ Schedule seeding complete");
        await Task.Delay(3000);

        // Seed services and capture the result
        Console.WriteLine("🎯 Seeding services...");
        var seededServices = await ServiceSeeder.ReseedForTestingAsync(app.Services);
        Console.WriteLine($"✅ Service seeding complete - {seededServices.Count} services created");
        await Task.Delay(3000);

        // Seed auth
        Console.WriteLine("🔐 Seeding auth...");
        await AuthSeeder.ReseedSeedAsync(app.Services);
        Console.WriteLine("✅ Auth seeding complete");
        await Task.Delay(3000);

        // Seed recommendations with the users and services
        Console.WriteLine("💡 Seeding recommendations...");
        await RecommendationSeeder.ReseedForTestingAsync(app.Services, seededUsers, seededServices);
        Console.WriteLine("✅ Recommendation seeding complete");

        Console.WriteLine("🎉 All seeding complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Seeding failed: {ex.Message}");
        Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
        throw new InvalidOperationException("Database seeding failed", ex);
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
