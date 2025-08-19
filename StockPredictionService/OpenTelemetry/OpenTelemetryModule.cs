using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using StockPredictionService.OpenTelemetry.Services;
using StockPredictionService.Services.Interfaces;

namespace StockPredictionService.OpenTelemetry;

public static class OpenTelemetryModule
{
    private static readonly HashSet<string> RegisteredModules = [];

    public static IServiceCollection AddOpenTelemetryMetricsModule(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] Adding OpenTelemetry metrics module");
        services.AddSingleton<IMetricsServiceFactory, OpenTelemetryMetricsServiceFactory>();
        services.AddSingleton<IMetricsService>(provider =>
        {
            var factory = provider.GetRequiredService<IMetricsServiceFactory>();
            return factory.CreateMetricsService("StockPrediction");
        });
        return services;
    }

    public static IServiceCollection RegisterModuleForMetrics(this IServiceCollection services, string moduleName)
    {
        var meterName = $"TBD.{moduleName}";
        RegisteredModules.Add(meterName);
        Console.WriteLine($"[METRICS] Registered module meter: {meterName}");
        return services;
    }

    private static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] Configuring OpenTelemetry with both metrics and tracing");
        Console.WriteLine($"[METRICS] Registered modules: {string.Join(", ", RegisteredModules)}");

        if (RegisteredModules.Count == 0)
        {
            Console.WriteLine("[METRICS] ⚠️ WARNING: No modules registered for metrics!");
        }

        services.AddOpenTelemetry()
    .WithMetrics(builder =>
    {
        // Register your meters
        foreach (var meterName in RegisteredModules)
        {
            builder.AddMeter(meterName);
        }

        builder
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation() // works after adding the package
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddPrometheusExporter(options =>
            {
                options.ScrapeEndpointPath = "/metrics";
                options.ScrapeResponseCacheDurationMilliseconds = 0;
            });
    })
    .WithTracing(builder =>
    {
        builder
            .AddSource("TBD.UserModule.DataSeeder")
            .AddSource("TBD.StockPrediction")
            .AddSource("TBD.StockPipeline")
            .AddSource("TBD.TestModule")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation(options =>
            {
                options.SetDbStatementForText = true;
                options.SetDbStatementForStoredProcedure = true;
            })
            .AddConsoleExporter();
    });


        return services;
    }

    // Keep the old method for backward compatibility
    public static IServiceCollection ConfigureOpenTelemetryMetrics(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] ConfigureOpenTelemetryMetrics is deprecated. Use ConfigureOpenTelemetry instead.");
        return ConfigureOpenTelemetry(services);
    }
}
