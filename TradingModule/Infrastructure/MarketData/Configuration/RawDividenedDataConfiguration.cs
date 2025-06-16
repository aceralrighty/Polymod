using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData.Configuration;

public class RawDividenedDataConfiguration: IEntityTypeConfiguration<RawDividendData>
{
    public void Configure(EntityTypeBuilder<RawDividendData> entity)
    {
        entity.HasKey(e => e.DividendId);
        entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
        entity.HasIndex(e => new { e.Symbol, e.ExDividendDate }).IsUnique();
        entity.Property(e => e.Amount).HasPrecision(18,6).HasColumnType("decimal(18, 4)");
    }
}
