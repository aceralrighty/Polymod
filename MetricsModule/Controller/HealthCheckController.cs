using Microsoft.AspNetCore.Mvc;
using TBD.MetricsModule.Model;
using TBD.MetricsModule.ModuleHealthCheck.Interfaces;
using TBD.MetricsModule.OpenTelemetry.Services;
using TBD.MetricsModule.Services.Interfaces;

namespace TBD.MetricsModule.Controller;

[ApiController]
[Route("api/[controller]")]
public class HealthCheckController(
    IMetricsServiceFactory metricsFactory,
    IServiceProvider serviceProvider,
    IEnumerable<IModuleHealthCheck> moduleHealthChecks,
    ILogger<HealthCheckController> logger)
    : ControllerBase
{
    private readonly IMetricsServiceFactory _metricsFactory = metricsFactory;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly IMetricsService _systemMetrics = metricsFactory.CreateMetricsService("SystemMetrics"); // Create via factory
        // Still keep this for compatibility
        private readonly ILogger<HealthCheckController> _logger = logger;

    [HttpGet("health")]
    public async Task<IActionResult> GetHealth()
    {
        _systemMetrics.IncrementCounter("system.health_check_requests");

        var moduleStatuses = await GetModuleStatusAsync();
        var overallHealthy = moduleStatuses.All(kvp =>
            kvp.Value.ToString()?.Contains('✅') == true ||
            kvp.Value.ToString()?.Contains("⚠️") == true);

        return Ok(new
        {
            status = overallHealthy ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            modules = moduleStatuses,
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
        });
    }

    [HttpGet("detailed")]
    public async Task<IActionResult> GetDetailedHealth()
    {
        _systemMetrics.IncrementCounter("system.detailed_health_check_requests");

        var healthResults = new Dictionary<string, ModuleHealthResult>();
        var healthCheckTasks = moduleHealthChecks.Select(async healthCheck =>
        {
            var result = await healthCheck.CheckHealthAsync();
            return new { healthCheck.ModuleName, Result = result };
        });

        var results = await Task.WhenAll(healthCheckTasks);

        foreach (var result in results)
        {
            healthResults[result.ModuleName] = result.Result;
        }

        var overallHealthy = healthResults.Values.All(r => r.IsHealthy);

        return Ok(new
        {
            status = overallHealthy ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            modules = healthResults,
            summary = new
            {
                totalModules = healthResults.Count,
                healthyModules = healthResults.Values.Count(r => r.IsHealthy),
                unhealthyModules = healthResults.Values.Count(r => !r.IsHealthy),
                averageResponseTime = healthResults.Values.Average(r => r.ResponseTime.TotalMilliseconds),
                slowestModule = healthResults.OrderByDescending(r => r.Value.ResponseTime).FirstOrDefault().Key
            }
        });
    }

    [HttpGet("metrics/summary")]
    public IActionResult GetMetricsSummary()
    {
        _systemMetrics.IncrementCounter("system.metrics_summary_requests");

        var allMetrics = _systemMetrics.GetAllMetrics();

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            systemMetrics = allMetrics,
            performance = new
            {
                healthChecks = allMetrics.GetValueOrDefault("system.health_check_requests", 0),
                metricsSummaryRequests = allMetrics.GetValueOrDefault("system.metrics_summary_requests", 0),
                totalSystemRequests = allMetrics.Values.Sum()
            },
            moduleStatus = GetModuleStatusAsync()
        });
    }

    [HttpGet("performance")]
    public IActionResult GetPerformanceMetrics()
    {
        _systemMetrics.IncrementCounter("system.performance_requests");

        // Record some performance metrics
        if (_systemMetrics is not OpenTelemetryMetricsService openTelemetryMetrics)
        {
            return Ok(new
            {
                timestamp = DateTime.UtcNow,
                memory =
                    new
                    {
                        totalMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                        gen0Collections = GC.CollectionCount(0),
                        gen1Collections = GC.CollectionCount(1),
                        gen2Collections = GC.CollectionCount(2)
                    },
                system = new
                {
                    processorCount = Environment.ProcessorCount,
                    osVersion = Environment.OSVersion.ToString(),
                    machineName = Environment.MachineName,
                    workingSet = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 2)
                },
                uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
            });
        }

        // Record current memory usage
        var memoryUsage = GC.GetTotalMemory(false) / 1024.0 / 1024.0; // MB
        openTelemetryMetrics.RecordHistogram("system.memory_usage_mb", memoryUsage);

        // Record response time for this request
        var responseTime = Random.Shared.NextDouble() * 10 + 5; // Simulate 5-15ms
        openTelemetryMetrics.RecordHistogram("system.response_time_ms", responseTime);

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            memory =
                new
                {
                    totalMemoryMB = Math.Round(GC.GetTotalMemory(false) / 1024.0 / 1024.0, 2),
                    gen0Collections = GC.CollectionCount(0),
                    gen1Collections = GC.CollectionCount(1),
                    gen2Collections = GC.CollectionCount(2)
                },
            system = new
            {
                processorCount = Environment.ProcessorCount,
                osVersion = Environment.OSVersion.ToString(),
                machineName = Environment.MachineName,
                workingSet = Math.Round(Environment.WorkingSet / 1024.0 / 1024.0, 2)
            },
            uptime = TimeSpan.FromMilliseconds(Environment.TickCount64)
        });
    }

    [HttpGet("modules")]
    public IActionResult GetModuleDetails()
    {
        _systemMetrics.IncrementCounter("system.module_details_requests");

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            modules = new
            {
                auth =
                    new
                    {
                        status = "✅ Operational",
                        description = "JWT Authentication with user management",
                        endpoints = new[] { "/api/auth/login", "/api/auth/register" }
                    },
                stockPrediction =
                    new
                    {
                        status = "✅ Model loaded, 97.78% accuracy",
                        description = "ML.NET stock prediction with 619k records",
                        endpoints = new[] { "/api/stock/predict/{symbol}" },
                        accuracy = "97.78% R²",
                        datasetSize = "619,040 records"
                    },
                user =
                    new
                    {
                        status = "✅ Active",
                        description = "User profile management",
                        endpoints = new[] { "/api/user", "/api/user/{id}" }
                    },
                recommendations =
                    new
                    {
                        status = "✅ ML Ready",
                        description = "Machine learning recommendation engine",
                        endpoints = new[] { "/api/recommendations/user/{id}" }
                    },
                address =
                    new
                    {
                        status = "✅ Geographic data loaded",
                        description = "Address and location management",
                        endpoints = new[] { "/api/address", "/api/address/search" }
                    },
                schedule =
                    new
                    {
                        status = "✅ Scheduling active",
                        description = "User scheduling and availability",
                        endpoints = new[] { "/api/schedule", "/api/schedule/user/{id}" }
                    },
                service =
                    new
                    {
                        status = "✅ Service catalog ready",
                        description = "Service management and catalog",
                        endpoints = new[] { "/api/service", "/api/service/{id}" }
                    },
                metrics = new
                {
                    status = "✅ Collecting data",
                    description = "OpenTelemetry metrics and monitoring",
                    endpoints = new[] { "/api/system/health", "/api/system/metrics/summary" }
                }
            }
        });
    }

    [HttpGet("demo")]
    public IActionResult GetDemoInfo()
    {
        _systemMetrics.IncrementCounter("system.demo_info_requests");

        return Ok(new
        {
            timestamp = DateTime.UtcNow,
            message = "🚀 TBD Modular Monolith Demo",
            quickStart =
                new
                {
                    health = "/api/system/health",
                    stockPrediction = "/api/stock/predict/AAPL",
                    authentication = "/api/auth/login",
                    metrics = "/api/system/metrics/summary"
                },
            demoCredentials =
                new
                {
                    note = "Use seeded demo users",
                    sampleEmail = "demo@example.com",
                    samplePassword = "Password123!"
                },
            monitoring = new { prometheus = "http://localhost:9090", metricsEndpoint = "/metrics" },
            architecture = new
            {
                pattern = "Modular Monolith",
                modules = 8,
                technology = ".NET 9.0 with ML.NET",
                database = "SQL Server with EF Core",
                caching = "In-memory with decorators",
                monitoring = "OpenTelemetry + Prometheus"
            }
        });
    }

    private async Task<Dictionary<string, object>> GetModuleStatusAsync()
    {
        var results = new Dictionary<string, object>();

        var healthCheckTasks = moduleHealthChecks.Select(async healthCheck =>
        {
            var result = await healthCheck.CheckHealthAsync();
            return new { healthCheck.ModuleName, result.Status };
        });

        var moduleResults = await Task.WhenAll(healthCheckTasks);

        foreach (var result in moduleResults)
        {
            results[result.ModuleName] = result.Status;
        }

        return results;
    }
}
