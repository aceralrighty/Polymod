using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry;

public static class OpenTelemetryModule
{
    private static readonly HashSet<string> RegisteredModules = [];

    public static IServiceCollection AddOpenTelemetryMetricsModule(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] Adding OpenTelemetry metrics module");
        services.AddSingleton<IMetricsServiceFactory, OpenTelemetryMetricsServiceFactory>();
        services.AddSingleton<IMetricsService>(_ => new OpenTelemetryMetricsService("System"));
        return services;
    }

    public static IServiceCollection RegisterModuleForMetrics(this IServiceCollection services, string moduleName)
    {
        var meterName = $"TBD.{moduleName}";
        RegisteredModules.Add(meterName);
        Console.WriteLine($"[METRICS] Registered module meter: {meterName}");
        return services;
    }

    public static IServiceCollection ConfigureOpenTelemetry(this IServiceCollection services)
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
                Console.WriteLine("[METRICS] Building OpenTelemetry metrics...");

                // Add all registered module meters
                foreach (var meterName in RegisteredModules)
                {
                    Console.WriteLine($"[METRICS] Adding meter to OpenTelemetry: {meterName}");
                    builder.AddMeter(meterName);
                }

                // Also add the static meters that are created directly in your code
                builder.AddMeter("TBD.StockPrediction");
                builder.AddMeter("TBD.StockPipeline");
                builder.AddMeter("TBD.TestModule");
                builder.AddMeter("TBD.UserModule"); // Add UserModule meter
                Console.WriteLine("[METRICS] Added static meters: TBD.StockPrediction, TBD.StockPipeline, TBD.TestModule, TBD.UserModule");

                builder
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddPrometheusExporter(options =>
                    {
                        Console.WriteLine("[METRICS] Configuring Prometheus exporter");
                        options.ScrapeEndpointPath = "/metrics";
                        options.ScrapeResponseCacheDurationMilliseconds = 0; // Disable caching for real-time metrics
                        Console.WriteLine("[METRICS] Prometheus exporter configured with endpoint: /metrics");
                    });

                Console.WriteLine("[METRICS] OpenTelemetry metrics configuration complete");
            })
            .WithTracing(builder =>
            {
                Console.WriteLine("[TRACING] Building OpenTelemetry tracing...");

                builder
                    .AddSource("TBD.UserModule.DataSeeder") // Add our DataSeeder activity source
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
                    .AddConsoleExporter(); // For development - replace with a proper exporter for production


                Console.WriteLine("[TRACING] OpenTelemetry tracing configuration complete");
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
