using OpenTelemetry.Metrics;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.OpenTelemetry;

public static class OpenTelemetryModule
{
    public static IServiceCollection AddOpenTelemetryMetricsModule(this IServiceCollection services)
    {
        services.AddSingleton<IMetricsServiceFactory, OpenTelemetryMetricsServiceFactory>();

        // Configure OpenTelemetry with Prometheus exporter
        services.AddOpenTelemetry()
            .WithMetrics(builder =>
            {
                builder
                    .AddMeter("TBD.StockPrediction")
                    .AddMeter("TBD.*") // Add all TBD meters
                    .AddRuntimeInstrumentation()
                    .AddProcessInstrumentation()
                    .AddPrometheusExporter();
            });

        return services;
    }
}
