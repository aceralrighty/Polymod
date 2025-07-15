using OpenTelemetry.Trace;
using TBD.AddressModule;
using TBD.AuthModule;
using TBD.AuthModule.Seed;
using TBD.MetricsModule;
using TBD.MetricsModule.ModuleHealthCheck;
using TBD.MetricsModule.OpenTelemetry;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.RecommendationModule;
using TBD.RecommendationModule.Seed;
using TBD.ScheduleModule;
using TBD.ScheduleModule.Seed;
using TBD.ServiceModule;
using TBD.ServiceModule.Seed;
using TBD.Shared.EntityMappers;
using TBD.StockPredictionModule;
using TBD.StockPredictionModule.PipelineOrchestrator;
using TBD.UserModule;
using TBD.UserModule.Seed;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMetricsModule();
builder.Services.AddOpenTelemetryMetricsModule();
builder.Services.AddHealthModuleChecks();
builder.Services.RegisterModuleForMetrics("Auth");
builder.Services.RegisterModuleForMetrics("StockPrediction");
builder.Services.RegisterModuleForMetrics("User");
builder.Services.RegisterModuleForMetrics("Recommendations");
builder.Services.RegisterModuleForMetrics("Address");
builder.Services.RegisterModuleForMetrics("Schedule");
builder.Services.RegisterModuleForMetrics("Service");
builder.Services.RegisterModuleForMetrics("Metrics");
// DI Containers
builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddServiceModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddRecommendationModule(builder.Configuration);
builder.Services.AddStockModule(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
// Extension method to call the autoMappers for my modules
builder.Services.AddAutoMapperExtension();
builder.Services.ConfigureOpenTelemetryMetrics();
builder.Services.AddMemoryCache();

builder.Services.AddOpenTelemetry()
    .WithTracing(providerBuilder => providerBuilder
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

var app = builder.Build();
app.UseOpenTelemetryPrometheusScrapingEndpoint();


if (app.Environment.IsDevelopment())
{
    try
    {
        Console.WriteLine("🌱 Starting database seeding and model training...");

        using (var scope = app.Services.CreateScope())
        {
            var scopedServices = scope.ServiceProvider;
            var testFactory = scopedServices.GetRequiredService<IMetricsServiceFactory>();
            var testMetrics = testFactory.CreateMetricsService("TestModule");

            Console.WriteLine("🔍 Testing basic counter...");
            testMetrics.IncrementCounter("test.immediate_counter");
            testMetrics.IncrementCounter("test.immediate_counter");
            testMetrics.IncrementCounter("test.immediate_counter");

            Console.WriteLine("🔍 Testing histogram...");
            if (testMetrics is OpenTelemetryMetricsService openTelemetryTest)
            {
                openTelemetryTest.RecordHistogram("test.immediate_histogram", 42.0);
                openTelemetryTest.RecordHistogram("test.immediate_histogram", 84.0);
            }

            Console.WriteLine("✅ Test metrics recorded");

            // Wait a moment for metrics to be processed
            await Task.Delay(2000);


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
            await Task.Delay(1000);

            var prediction = scopedServices.GetRequiredService<StockPredictionPipeline>();
            await prediction.ExecuteFullPipelineAsync("StockPredictionModule/Dataset/all_stocks_5yr.csv");
            Console.WriteLine("✅ Prediction complete!");
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
