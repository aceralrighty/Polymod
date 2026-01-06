using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PolyMod.AddressModule.Models;

namespace PolyMod.AddressModule.Data;

public class UserAddressConfig : IEntityTypeConfiguration<UserAddress>
{
    public void Configure(EntityTypeBuilder<UserAddress> builder)
    {
        builder.HasOne(u => u.User).WithOne().HasForeignKey<UserAddress>(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.Id).IsUnique();
        builder.Property(x => x.CreatedAt).ValueGeneratedOnAdd().Metadata
            .SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        builder.Property(x => x.UserId).IsRequired();

        builder.Property(x => x.Address1).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Address2).HasMaxLength(255);
        builder.Property(x => x.City).HasMaxLength(255).IsRequired();
        builder.Property(x => x.State).HasMaxLength(255).IsRequired();
        builder.Property(x => x.ZipCode).HasMaxLength(10).IsRequired();
    }
}
