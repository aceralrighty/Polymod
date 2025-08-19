namespace StockPredictionService.MetricsModule;

internal interface IModuleHealthCheck
{
    string ModuleName { get; }
    Task<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
