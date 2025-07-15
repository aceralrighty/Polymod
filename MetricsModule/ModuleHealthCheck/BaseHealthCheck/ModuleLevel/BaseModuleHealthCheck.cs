using System.Diagnostics;
using TBD.MetricsModule.Model;
using TBD.MetricsModule.ModuleHealthCheck.Interfaces;

namespace TBD.MetricsModule.ModuleHealthCheck.BaseHealthCheck.ModuleLevel;

public abstract class BaseModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : IModuleHealthCheck
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider;
    protected readonly ILogger<BaseModuleHealthCheck> _logger = logger;

    public abstract string ModuleName { get; }

    public async Task<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogDebug("Starting health check for {ModuleName}", ModuleName);

            var result = await PerformHealthCheckAsync(cancellationToken);
            result.ResponseTime = stopwatch.Elapsed;

            _logger.LogDebug("Health check completed for {ModuleName}: {Status}", ModuleName, result.Status);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for {ModuleName}", ModuleName);

            return new ModuleHealthResult
            {
                Status = "❌ Error",
                Description = $"{ModuleName} module error: {ex.Message}",
                IsHealthy = false,
                ResponseTime = stopwatch.Elapsed,
                Exception = ex,
                Endpoints = GetEndpoints()
            };
        }
    }

    protected abstract Task<ModuleHealthResult> PerformHealthCheckAsync(CancellationToken cancellationToken);
    protected abstract string[] GetEndpoints();
}
