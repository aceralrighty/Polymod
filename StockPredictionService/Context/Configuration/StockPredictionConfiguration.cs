using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StockPredictionService.Models.Stocks;

namespace StockPredictionService.Context.Configuration;

public class StockPredictionConfiguration : IEntityTypeConfiguration<StockPrediction>
{
    public void Configure(EntityTypeBuilder<StockPrediction> builder)
    {
        builder.Property(s => s.PredictedPrice).HasColumnType("decimal(18, 4)").HasPrecision(18, 2);
    }
}
