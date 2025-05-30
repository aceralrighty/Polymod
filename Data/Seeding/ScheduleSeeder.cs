using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.Data.Seeding;

public static class ScheduleSeeder
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
                Password = Hasher.HashPassword("<PASSWORD>"),
                Email = "alice@example.com",
                Schedule = new Schedule()
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
                Password = Hasher.HashPassword("AsAnIrishPotato"),
                Email = "bob@example.com",
                Schedule = new Schedule()
            }
        };
        schedule2.UserId = schedule2.User.Id;
        schedule2.RecalculateTotalHours();
        var schedule3 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 }, { "Tuesday", 10 }, { "Wednesday", 10 },
                { "Thursday", 8 }, { "Friday", 10 }, { "Saturday", 0 }, { "Sunday", 0 }
            },
            BasePay = 28,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "Adam",
                Password = Hasher.HashPassword("PotatoLover"),
                Email = "Adam@example.com",
                Schedule = new Schedule()
            }
        };
        schedule3.UserId = schedule3.User.Id;
        schedule3.RecalculateTotalHours();
        schedules.Add(schedule1);
        schedules.Add(schedule2);
        schedules.Add(schedule3);
        await scheduleContext.Schedules.AddRangeAsync(schedules);
        await scheduleContext.SaveChangesAsync();
    }
}