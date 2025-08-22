using System.Diagnostics;

namespace StockPredictionService.MetricsModule;

public abstract class BaseModuleHealthCheck(IServiceProvider serviceProvider, ILogger<BaseModuleHealthCheck> logger)
    : IModuleHealthCheck
{
    protected readonly IServiceProvider ServiceProvider = serviceProvider;

    public abstract string ModuleName { get; }

    public async Task<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogDebug("Starting health check for {ModuleName}", ModuleName);

            var result = await PerformHealthCheckAsync(cancellationToken);
            result.ResponseTime = stopwatch.Elapsed;

            logger.LogDebug("Health check completed for {ModuleName}: {Status}", ModuleName, result.Status);
            return result;
        }
        catch (InvalidOperationException ex)
        {
            logger.LogError(ex, "Health check failed for {ModuleName}", ModuleName);

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
