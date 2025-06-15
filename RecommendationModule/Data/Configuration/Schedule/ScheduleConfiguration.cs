using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace TBD.RecommendationModule.Data.Configuration.Schedule;

public class ScheduleConfiguration: IEntityTypeConfiguration<ScheduleModule.Models.Schedule>
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

        // Ignore the computed properties since they're not mapped
        builder.Ignore(s => s.DaysWorked);
        builder.Ignore(s => s.TotalPayComputed);

        // ⭐ FIXED: Use HasColumnType("real") for float properties
        builder.Property(s => s.BasePay)
            .HasColumnType("real"); // For float, use "real" not HasPrecision

        builder.Property(s => s.TotalHoursWorked)
            .HasColumnType("real"); // Add this for TotalHoursWorked too

        builder.Property(s => s.TotalPay)
            .HasColumnType("real") // For float, use "real" not HasPrecision
            .HasComputedColumnSql(
                "CASE " +
                // No overtime (≤40 hours)
                "WHEN [TotalHoursWorked] <= 40 THEN [BasePay] * [TotalHoursWorked] " +
                // Regular overtime only (41-60 hours): 1.5x rate
                "WHEN [TotalHoursWorked] <= 60 THEN " +
                "([BasePay] * 40) + " + // Regular pay for the first 40 hours
                "(([BasePay] * 1.5) * ([TotalHoursWorked] - 40)) " + // 1.5x for hours 41-60
                // Double overtime (61+ hours): 1.5x for 41-60, 2x for 61+
                "ELSE " +
                "([BasePay] * 40) + " + // Regular pay for the first 40 hours
                "(([BasePay] * 1.5) * 20) + " + // 1.5x for hours 41-60 (20-hour max)
                "(([BasePay] * 2.0) * ([TotalHoursWorked] - 60)) " + // 2x for hours 61+
                "END"
            );
    }
}
