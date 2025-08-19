using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyMod.StockPredictionService.Models;

namespace PolyMod.StockPredictionService.Context.Configuration;

public class RawDataConfiguration: IEntityTypeConfiguration<RawData>
{
    public void Configure(EntityTypeBuilder<RawData> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Symbol).HasMaxLength(10);

    }
}
