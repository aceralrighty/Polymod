using TBD.GenericDBProperties;

namespace TBD.TradingModule.Preprocessing;

public class LabelGenerator : BaseTableProperties
{
    public float NextDayReturn { get; set; }
    public float VolatilityScore { get; set; }
    public float SharpeRatio { get; set; }
    public bool IsHighReturnLowRisk { get; set; }
}
