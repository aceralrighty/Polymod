using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyMod.RecommendationModule.Models.Recommendations;

namespace PolyMod.RecommendationModule.Data.Configuration.Recommendation;

public class UserRecommendationConfiguration : IEntityTypeConfiguration<UserRecommendation>
{
    public void Configure(EntityTypeBuilder<UserRecommendation> builder)
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
    }
}
