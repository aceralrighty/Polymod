using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.RecommendationModule.Models;

namespace TBD.RecommendationModule.Data.Configuration.Recommendation;

public class RecommendationOutputConfiguration : IEntityTypeConfiguration<RecommendationOutput>
{
    public void Configure(EntityTypeBuilder<RecommendationOutput> builder)
    {
        builder.HasKey(r => r.Id);

        builder.HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Service)
            .WithMany()
            .HasForeignKey(r => r.ServiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for common queries
        builder.HasIndex(r => new { r.UserId, r.GeneratedAt })
            .HasDatabaseName("IX_RecommendationOutputs_UserId_GeneratedAt");

        builder.HasIndex(r => r.BatchId)
            .HasDatabaseName("IX_RecommendationOutputs_BatchId");

        // Configure precision for score
        builder.Property(r => r.Score).HasColumnType("decimal(18,2)");
    }
}
