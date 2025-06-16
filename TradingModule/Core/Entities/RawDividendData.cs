using TBD.GenericDBProperties;

namespace TBD.TradingModule.Core.Entities;

public class RawDividendData : BaseTableProperties
{
    public int DividendId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime ExDividendDate { get; set; }
    public decimal Amount { get; set; }
    public DateTime? PaymentDate { get; set; }
    public DateTime? RecordDate { get; set; }
    public DateTime? DeclarationDate { get; set; }
}
