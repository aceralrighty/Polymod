using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.TradingModule.Core.Entities;

namespace TBD.TradingModule.Infrastructure.MarketData.Configuration;

public class ApiRequestLogConfiguration : IEntityTypeConfiguration<ApiRequestLog>
{
    public void Configure(EntityTypeBuilder<ApiRequestLog> builder)
    {
        builder.HasKey(a => a.Id);
        builder.Property(a => a.ApiProvider).HasMaxLength(50);
        builder.Property(a => a.RequestType).HasMaxLength(50);
        builder.Property(a => a.Symbol).HasMaxLength(10);
        builder.Property(a => a.ErrorMessage).HasMaxLength(500);

        builder.HasIndex(a => new { a.ApiProvider, a.RequestTime });
    }
}
