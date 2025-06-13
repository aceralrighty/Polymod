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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
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
        Console.WriteLine("🌱 Starting database seeding and model training...");

        using (var scope = app.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;

            // Seed users first and capture the result
            Console.WriteLine("👥 Seeding users...");
            var seededUsers = await DataSeeder.ReseedForTestingAsync(scopedServices);
            Console.WriteLine($"✅ User seeding complete - {seededUsers.Count} users created");
            await Task.Delay(1000);

            // Seed schedules
            Console.WriteLine("📅 Seeding schedules...");
            await ScheduleSeeder.ReseedForTestingAsync(scopedServices);
            Console.WriteLine("✅ Schedule seeding complete");
            await Task.Delay(1000);

            // Seed services and capture the result
            Console.WriteLine("🎯 Seeding services...");
            var seededServices = await ServiceSeeder.ReseedForTestingAsync(scopedServices);
            Console.WriteLine($"✅ Service seeding complete - {seededServices.Count} services created");
            await Task.Delay(1000);

            // Seed auth
            Console.WriteLine("🔐 Seeding auth...");
            await AuthSeeder.ReseedSeedAsync(scopedServices);
            Console.WriteLine("✅ Auth seeding complete");
            await Task.Delay(1000);

            // This is the combined seeding and training logic
            // The RecommendationSeederAndTrainer will handle database recreation and data seeding for recommendations
            Console.WriteLine(
                "💡 Starting RecommendationSeederAndTrainer workflow (seeding recommendations and training model)...");
            var recommendationSeederAndTrainer = scopedServices.GetRequiredService<RecommendationSeederAndTrainer>();
            await recommendationSeederAndTrainer.SeedRecommendationsAsync(seededUsers, seededServices,
                includeRatings: true);
            Console.WriteLine("✅ Recommendation Seeding and Training complete!");
        }

        Console.WriteLine("🎉 All startup tasks complete!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Startup failed: {ex.Message}");
        Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
        throw new InvalidOperationException(
            "Application startup failed due to database seeding or model training issues", ex);
    }
}

app.MapOpenApi();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TBD.Api v1"));
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseRouting();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("=============================>Starting Server<=============================\n");
await app.RunAsync();
