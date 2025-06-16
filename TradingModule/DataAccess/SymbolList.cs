using TBD.GenericDBProperties;

namespace TBD.TradingModule.DataAccess;

public class SymbolList : BaseTableProperties
{
    public Guid SymbolListId { get; set; }
    public string Symbol { get; set; }
    public string CompanyName { get; set; }
    public string Sector { get; set; }
    public string Industry { get; set; }
    public decimal MarketCap { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AddedDate { get; set; }
    public DateTime? RemovedDate { get; set; }
}
