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
using TBD.Shared.Utils.EntityMappers;
using TBD.TradingModule;
using TBD.TradingModule.ML;
using TBD.TradingModule.Orchestration;
using TBD.UserModule;
using TBD.UserModule.Seed;
using TBD.TradingModule.Preprocessing; // Add this using directive

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMetricsModule();
// DI Containers
builder.Services.AddUserService(builder.Configuration);
builder.Services.AddAddressService(builder.Configuration);
builder.Services.AddScheduleModule(builder.Configuration);
builder.Services.AddServiceModule(builder.Configuration);
builder.Services.AddAuthModule(builder.Configuration);
builder.Services.AddRecommendationModule(builder.Configuration);
builder.Services.AddTradingModule(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddControllersWithViews();
builder.Services.AddOpenApi();
// Extension method to call the autoMappers for my modules
builder.Services.AddAutoMapperExtension();

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
            await Task.Delay(1000);

            // --- Start of Trading Module Training Pipeline Fix ---
            var trainingOrchestrator = scopedServices.GetRequiredService<TrainingOrchestrator>();
            var featureEngineeringService = scopedServices.GetRequiredService<FeatureEngineeringService>();
            var stockPredictionEngine = scopedServices.GetRequiredService<StockPredictionEngine>();

            var csvPath = Path.Combine(builder.Environment.ContentRootPath, "Dataset", "all_stocks_5yr.csv");

            try
            {
                Console.WriteLine($"📈 Loading training data from: {csvPath}");
                var marketData = await trainingOrchestrator.LoadTrainingDataFromCsvAsync(csvPath);

                if (marketData.Count != 0)
                {
                    Console.WriteLine("⚙️ Generating feature sets...");
                    var allFeatureSets = new List<FeatureEngineeringService.FeatureSet>();
                    foreach (var symbolData in marketData.Values)
                    {
                        try
                        {
                            var featureSetsForSymbol = featureEngineeringService.GenerateFeatureSets(symbolData);
                            allFeatureSets.AddRange(featureSetsForSymbol);
                        }
                        catch (ArgumentException ex)
                        {
                            Console.WriteLine($"⚠️ Skipping feature generation for a symbol due to: {ex.Message}");
                        }
                    }

                    if (allFeatureSets.Any())
                    {
                        Console.WriteLine($"🚀 Training and predicting with {allFeatureSets.Count} feature sets...");
                        var predictions = await stockPredictionEngine.TrainAndPredictAsync(allFeatureSets);
                        Console.WriteLine($"✅ Prediction complete. Generated {predictions.Count} predictions.");
                    }
                    else
                    {
                        Console.WriteLine("❌ No feature sets generated for training.");
                    }
                }
                else
                {
                    Console.WriteLine("⚠️ No market data loaded for training.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during stock prediction model training: {ex.Message}");
                Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
            }
            // --- End of Trading Module Training Pipeline Fix ---
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
