namespace TBD.TradingModule.Orchestration.Supporting;

public class ApiUsageStatus
{
    public int RemainingRequests { get; set; }
    public bool CanMakeRequests { get; set; }
    public DateTime ResetTime { get; set; }
}
