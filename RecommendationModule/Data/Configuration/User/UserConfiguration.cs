using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TBD.ScheduleModule.Models;

namespace TBD.RecommendationModule.Data.Configuration.User;

public class UserConfiguration : IEntityTypeConfiguration<UserModule.Models.User>
{
    public void Configure(EntityTypeBuilder<UserModule.Models.User> builder)
    {
        builder.HasKey(u => u.Id);

        builder.HasOne(u => u.Schedule)
            .WithOne(s => s.User)
            .HasForeignKey<ScheduleModule.Models.Schedule>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
