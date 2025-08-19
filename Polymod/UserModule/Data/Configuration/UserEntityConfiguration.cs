using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyMod.UserModule.Models;
using PolyMod.ScheduleModule.Models;

namespace PolyMod.UserModule.Data.Configuration;

public class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasIndex(u => u.Email)
            .IsUnique();

        builder
            .HasIndex(u => u.Username)
            .IsUnique();

        builder
            .Property(u => u.CreatedAt)
            .ValueGeneratedOnAdd()
            .Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);

    }
}
