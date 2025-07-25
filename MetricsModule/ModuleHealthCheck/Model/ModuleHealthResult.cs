namespace TBD.MetricsModule.ModuleHealthCheck.Model;

public class ModuleHealthResult
{
    public string Status { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalData { get; set; } = new();
    public bool IsHealthy { get; set; }
    public TimeSpan ResponseTime { get; set; }
    public string[] Endpoints { get; set; } = [];
    public Exception? Exception { get; set; }
}
