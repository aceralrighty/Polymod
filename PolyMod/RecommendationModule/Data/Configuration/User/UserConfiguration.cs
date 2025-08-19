using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TBD.RecommendationModule.Data.Configuration.User;

public class UserConfiguration : IEntityTypeConfiguration<UserModule.Models.User>
{
    public void Configure(EntityTypeBuilder<UserModule.Models.User> builder)
    {
        builder.HasKey(u => u.Id);

    }
}
