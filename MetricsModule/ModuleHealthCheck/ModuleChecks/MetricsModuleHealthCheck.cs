using TBD.MetricsModule.Model;
using TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.ModuleHealthCheck.ModuleChecks;

public class MetricsModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : BaseModuleHealthCheck(serviceProvider, logger)
{
    public override string ModuleName => "metrics";

    protected override Task<ModuleHealthResult> PerformHealthCheckAsync(CancellationToken cancellationToken)
    {
        var metricsService = ServiceProvider.GetService<IMetricsServiceFactory>();

        if (metricsService == null)
        {
            return Task.FromResult(new ModuleHealthResult
            {
                Status = "❌ Service not available",
                Description = "Metrics service not registered",
                IsHealthy = false,
                Endpoints = GetEndpoints()
            });
        }

        // Test metrics collection
        var systemMetrics = metricsService.CreateMetricsService("HealthCheck");
        systemMetrics.IncrementCounter("health_check.metrics_test");

        return Task.FromResult(new ModuleHealthResult
        {
            Status = "✅ Collecting data",
            Description = "OpenTelemetry metrics and monitoring",
            IsHealthy = true,
            Endpoints = GetEndpoints(),
            AdditionalData = new Dictionary<string, object>
            {
                { "metricsCollecting", true },
                { "openTelemetryActive", true }
            }
        });
    }

    protected override string[] GetEndpoints()
    {
        return new[] { "/api/system/health", "/api/system/metrics/summary", "/metrics" };
    }
}
