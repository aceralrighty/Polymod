using TBD.MetricsModule.Model;

namespace TBD.MetricsModule.ModuleHealthCheck.Interfaces;

public interface IModuleHealthCheck
{
    string ModuleName { get; }
    Task<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
