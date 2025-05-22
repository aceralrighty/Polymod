using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.Data.Seeding;

public class ScheduleSeeder
{
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scheduleContext = scope.ServiceProvider.GetRequiredService<ScheduleDbContext>();
        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();

        await userContext.Database.EnsureDeletedAsync();
        await scheduleContext.Database.EnsureDeletedAsync();
        await userContext.Database.MigrateAsync();
        await scheduleContext.Database.MigrateAsync();

        await SeedScheduleAsync(scheduleContext);
    }

    private static async Task SeedScheduleAsync(ScheduleDbContext scheduleContext)
    {
        var schedules = new List<Schedule>();

        var schedule1 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 8 }, { "Wednesday", 8 },
                { "Thursday", 8 }, { "Friday", 8 }, { "Saturday", 0 }, { "Sunday", 0 }
            },
            BasePay = 20,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "alice",
                Email = "alice@example.com"
            }
        };
        schedule1.UserId = schedule1.User.Id;
        schedule1.RecalculateTotalHours();

        var schedule2 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 0 }, { "Tuesday", 0 }, { "Wednesday", 4 },
                { "Thursday", 6 }, { "Friday", 6 }, { "Saturday", 10 }, { "Sunday", 10 }
            },
            BasePay = 22,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "bob",
                Email = "bob@example.com"
            }
        };
        schedule2.UserId = schedule2.User.Id;
        schedule2.RecalculateTotalHours();

        schedules.Add(schedule1);
        schedules.Add(schedule2);

        await scheduleContext.Schedules.AddRangeAsync(schedules);
        await scheduleContext.SaveChangesAsync();
    }
}