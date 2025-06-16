using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.DataAccess;

namespace TBD.TradingModule.MarketData.Configuration;

public class StockFeatureVectorConfiguration : IEntityTypeConfiguration<StockFeatureVector>
{
    public void Configure(EntityTypeBuilder<StockFeatureVector> builder)
    {
        builder.HasKey(s => new { s.Symbol, s.Date });
        builder.Property(s => s.Symbol).HasMaxLength(10);
        builder.HasIndex(s => s.Date);
        builder.HasIndex(s => s.Symbol);
        builder.HasIndex(s => new { s.Symbol, s.Date });
    }
}
