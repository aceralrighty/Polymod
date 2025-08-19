using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TBD.RecommendationModule.Data.Configuration.Service;

public class ServiceConfiguration : IEntityTypeConfiguration<ServiceModule.Models.Service>
{
    public void Configure(EntityTypeBuilder<ServiceModule.Models.Service> builder)
    {
        // Configure the primary key if not already configured elsewhere
        builder.HasKey(s => s.Id);

        // Configure decimal properties with appropriate precision and scale
        builder.Property(s => s.Price)
            .HasPrecision(18, 2);

        builder.Property(s => s.TotalPrice)
            .HasPrecision(18, 2);


    }
}
