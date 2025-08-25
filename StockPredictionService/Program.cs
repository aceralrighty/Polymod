using StockPredictionService;
using StockPredictionService.Context;
using StockPredictionService.PipelineOrchestrator;
using StockPredictionService.Services;
using StockPredictionService.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add your Stock Module (this is your existing DI container)
builder.Services.AddStockModule(builder.Configuration);

// Add simple metrics service to replace OpenTelemetry dependency
builder.Services.AddSingleton<IMetricsServiceFactory, MetricsServiceFactory>();

// FIX: Use the factory to create the MetricsService instead of directly registering it
builder.Services.AddSingleton<IMetricsService>(provider =>
{
    var factory = provider.GetRequiredService<IMetricsServiceFactory>();
    var configuration = provider.GetRequiredService<IConfiguration>();
    var serviceName = configuration["ServiceName"] ?? "StockPredictionService";
    return factory.CreateMetricsService(serviceName);
});

// Add other essential services that might be missing from your module
builder.Services.AddAutoMapper(typeof(Program)); // If you need AutoMapper
builder.Services.AddMemoryCache();
builder.Services.AddLogging();

// If you need CORS for your microservice
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "TBD.Api v1"));

    // Optional: Run your stock prediction pipeline on startup in dev
    try
    {
        Console.WriteLine("🔮 Starting Stock Prediction Service...");

        using var scope = app.Services.CreateScope();
        var scopedServices = scope.ServiceProvider;

        // Ensure database is created
        var context = scopedServices.GetRequiredService<StockDbContext>();
        await context.Database.EnsureCreatedAsync().ConfigureAwait(false);

        // Optionally run the prediction pipeline
        var prediction = scopedServices.GetRequiredService<StockPredictionPipeline>();
        await prediction.ExecuteFullPipelineAsync("Dataset/all_stocks_5yr.csv").ConfigureAwait(false);
        Console.WriteLine("✅ Stock Prediction Service ready!");
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"⚠️ Startup warning: {ex.Message}");
        // Don't throw here - let the service start even if prediction fails
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.UseCors();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();

Console.WriteLine("🚀 Stock Prediction Service started!");
await app.RunAsync().ConfigureAwait(false);
