using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.Core.Entities;
using TBD.TradingModule.DataAccess;

namespace TBD.TradingModule.MarketData.Configuration;

public class PredictionResultConfiguration : IEntityTypeConfiguration<PredictionResult>
{
    public void Configure(EntityTypeBuilder<PredictionResult> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Symbol).HasMaxLength(10);
        builder.Property(r => r.ModelVersion).HasMaxLength(50);
        builder.HasIndex(r => r.PredictionDate);
        builder.HasIndex(r => r.TargetDate);
        builder.HasIndex(r => new { r.Symbol, r.TargetDate });
        builder.HasIndex(r => r.RiskAdjustedScore);
    }
}
