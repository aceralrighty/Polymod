using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData.Configuration;

public class RawDataConfiguration : IEntityTypeConfiguration<RawMarketData>
{
    public void Configure(EntityTypeBuilder<RawMarketData> entity)
    {
        entity.HasKey(e => e.MarketId);
        entity.HasIndex(e => new { e.Symbol, e.Date }).IsUnique();
        entity.Property(e => e.Symbol).IsRequired().HasMaxLength(10);
    }
}
