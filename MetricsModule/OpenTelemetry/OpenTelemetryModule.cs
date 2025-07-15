using OpenTelemetry.Metrics;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry;

public static class OpenTelemetryModule
{
    private static readonly HashSet<string> RegisteredModules = new();

    public static IServiceCollection AddOpenTelemetryMetricsModule(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] Adding OpenTelemetry metrics module");
        services.AddSingleton<IMetricsServiceFactory, OpenTelemetryMetricsServiceFactory>();
        return services;
    }

    public static IServiceCollection RegisterModuleForMetrics(this IServiceCollection services, string moduleName)
    {
        var meterName = $"TBD.{moduleName}";
        RegisteredModules.Add(meterName);
        Console.WriteLine($"[METRICS] Registered module meter: {meterName}");
        return services;
    }

    public static IServiceCollection ConfigureOpenTelemetryMetrics(this IServiceCollection services)
    {
        Console.WriteLine("[METRICS] Configuring OpenTelemetry metrics");
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
                Console.WriteLine("[METRICS] Added static meters: TBD.StockPrediction, TBD.StockPipeline, TBD.TestModule");

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
            });

        return services;
    }
}
