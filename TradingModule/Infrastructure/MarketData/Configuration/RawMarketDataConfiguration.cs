using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.DataAccess;

namespace TBD.TradingModule.MarketData.Configuration;

public class RawMarketDataConfiguration : IEntityTypeConfiguration<RawMarketData>
{
    public void Configure(EntityTypeBuilder<RawMarketData> builder)
    {
        builder.HasKey(r => r.MarketId);
        builder.HasIndex(r =>
            new { r.Symbol, r.Date }).IsUnique();
        builder.Property(r => r.Symbol).HasMaxLength(10);
        builder.Property(r => r.Open).HasPrecision(18, 4);
        builder.Property(r => r.High).HasPrecision(18, 4);
        builder.Property(r => r.Low).HasPrecision(18, 4);
        builder.Property(r => r.Close).HasPrecision(18, 4);
        builder.Property(r => r.AdjustedClose).HasPrecision(18, 4);

        builder.HasIndex(r => r.Date);
        builder.HasIndex(r => r.Symbol);
    }
}
