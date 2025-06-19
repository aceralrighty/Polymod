using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.StockPredictionModule.Models;

namespace TBD.StockPredictionModule.Context.Configuration;

public class StockPredictionConfiguration : IEntityTypeConfiguration<StockPrediction>
{
    public void Configure(EntityTypeBuilder<StockPrediction> builder)
    {
        builder.Property(s => s.Price).HasColumnType("decimal(18, 4)").HasPrecision(18, 2);
    }
}
