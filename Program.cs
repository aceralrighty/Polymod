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
using TBD.TradingModule.Infrastructure.MarketData;
using TBD.UserModule;
using TBD.UserModule.Seed;

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

            // ADD TRADING MODULE INITIALIZATION HERE
            Console.WriteLine("📈 Starting Trading Module initialization...");
            try
            {
                // Check the API key first
                var apiKey = builder.Configuration["API_KEY"];
                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine(
                        "⚠️  Warning: API_KEY environment variable not set. Trading module will not fetch real data.");
                    Console.WriteLine(
                        "   Set API_KEY environment variable with your Alpha Vantage API key to enable data fetching.");
                }
                else
                {
                    Console.WriteLine($"✅ API Key configured (length: {apiKey.Length})");
                }

                // Initialize dividend data fetcher
                var dividendFetcher = scopedServices.GetRequiredService<DividendDataFetcher>();
                var symbolsToFetch = new List<string> { "MSFT", "AAPL", "GOOGL" };
                var endDate = DateTime.Now;
                var startDate = endDate.AddYears(-2); // Get 2 years of dividend history

                Console.WriteLine($"🚀 Fetching dividend data for: {string.Join(", ", symbolsToFetch)}");
                Console.WriteLine($"📅 Date range: {startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}");

                var fetchedDividendData =
                    await dividendFetcher.GetBatchDividendDataAsync(symbolsToFetch, startDate, endDate);

                foreach (var entry in fetchedDividendData)
                {
                    Console.WriteLine($"  -> Fetched {entry.Value.Count} dividend records for {entry.Key}");
                    if (entry.Value.Count != 0)
                    {
                        var latest = entry.Value.OrderByDescending(d => d.ExDividendDate).First();
                        Console.WriteLine(
                            $"     Latest dividend: ${latest.Amount} on {latest.ExDividendDate:yyyy-MM-dd}");
                    }
                }

                // Check remaining API requests
                var remainingRequests = await dividendFetcher.GetRemainingRequestsAsync();
                Console.WriteLine($"📊 Remaining API requests this hour: {remainingRequests}");

                Console.WriteLine("✅ Trading Module initialization complete!");
            }
            catch (Exception tradingEx)
            {
                Console.WriteLine($"⚠️  Trading Module initialization failed: {tradingEx.Message}");
                Console.WriteLine(
                    "   This won't prevent the application from starting, but trading features may not work.");
                Console.WriteLine($"   Details: {tradingEx.StackTrace}");

                // Don't throw here - let the app continue without trading data
                // throw; // Uncomment this if you want trading failures to stop app startup
            }
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
