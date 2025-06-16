using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TBD.RecommendationModule.Data.Configuration.Schedule;

public class ScheduleConfiguration : IEntityTypeConfiguration<ScheduleModule.Models.Schedule>
{
    public void Configure(EntityTypeBuilder<ScheduleModule.Models.Schedule> builder)
    {
        builder.HasKey(s => s.Id);

        builder.HasOne(s => s.User)
            .WithOne(u => u.Schedule)
            .HasForeignKey<ScheduleModule.Models.Schedule>(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the JSON column for DaysWorked
        builder.Property(s => s.DaysWorkedJson)
            .HasColumnName("DaysWorkedJson")
            .HasColumnType("nvarchar(255)");

        builder.Ignore(s => s.TotalPayComputed);

        builder.Property(s => s.BasePay)
            .HasColumnType("real");

        builder.Property(s => s.TotalHoursWorked)
            .HasColumnType("real");

        builder.Property(s => s.TotalPay)
            .HasColumnType("real")
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [TotalHoursWorked] <= 40 THEN [BasePay] * [TotalHoursWorked] " +
                "WHEN [TotalHoursWorked] <= 60 THEN " +
                "([BasePay] * 40) + " +
                "(([BasePay] * 1.5) * ([TotalHoursWorked] - 40)) " +
                "ELSE " +
                "([BasePay] * 40) + " +
                "(([BasePay] * 1.5) * 20) + " +
                "(([BasePay] * 2.0) * ([TotalHoursWorked] - 60)) " +
                "END"
            );
    }
}
