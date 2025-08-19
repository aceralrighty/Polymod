using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace PolyMod.RecommendationModule.Data.Configuration.User;

public class UserConfiguration : IEntityTypeConfiguration<PolyMod.UserModule.Models.User>
{
    public void Configure(EntityTypeBuilder<PolyMod.UserModule.Models.User> builder)
    {
        builder.HasKey(u => u.Id);

    }
}
