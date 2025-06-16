using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.DataAccess;

namespace TBD.TradingModule.MarketData.Configuration;

public class SymbolListConfiguration : IEntityTypeConfiguration<SymbolList>
{
    public void Configure(EntityTypeBuilder<SymbolList> builder)
    {
        builder.HasKey(s => s.SymbolListId);
        builder.Property(s => s.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(s => s.CompanyName).HasMaxLength(200);
        builder.Property(s => s.Sector).HasMaxLength(100);
        builder.Property(s => s.Industry).HasMaxLength(100);
        builder.Property(s => s.MarketCap).HasPrecision(18, 2);
    }
}
