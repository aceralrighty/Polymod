using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PolyMod.Migrations.ScheduleDb
{
    /// <inheritdoc />
    public partial class InitSchedule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Schedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TotalHoursWorked = table.Column<float>(type: "real", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BasePay = table.Column<float>(type: "real", nullable: true),
                    TotalPay = table.Column<float>(type: "real", nullable: true, computedColumnSql: "CASE WHEN [TotalHoursWorked] <= 40 THEN [BasePay] * [TotalHoursWorked] WHEN [TotalHoursWorked] <= 60 THEN ([BasePay] * 40) + (([BasePay] * 1.5) * ([TotalHoursWorked] - 40)) ELSE ([BasePay] * 40) + (([BasePay] * 1.5) * 20) + (([BasePay] * 2.0) * ([TotalHoursWorked] - 60)) END"),
                    DaysWorkedJson = table.Column<string>(type: "varchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Schedules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Schedules_UserId",
                table: "Schedules",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schedules");
        }
    }
}
