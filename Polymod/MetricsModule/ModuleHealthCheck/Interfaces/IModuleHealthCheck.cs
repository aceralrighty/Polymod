using PolyMod.MetricsModule.ModuleHealthCheck.Model;

namespace PolyMod.MetricsModule.ModuleHealthCheck.Interfaces;

public interface IModuleHealthCheck
{
    string ModuleName { get; }
    Task<ModuleHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);
}
