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
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 8 },
                    { "Tuesday", 8 },
                    { "Wednesday", 8 },
                    { "Thursday", 8 },
                    { "Friday", 8 },
                    { "Saturday", 0 },
                    { "Sunday", 0 }
                },
            BasePay = 20,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "alice",
                Password = Hasher.HashPassword("AliceInWonderland"),
                Email = "alice@example.com",
                Schedule = new Schedule()
            }
        };
        schedule1.UserId = schedule1.User.Id;
        schedule1.RecalculateTotalHours();

        var schedule2 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 0 },
                    { "Wednesday", 4 },
                    { "Thursday", 6 },
                    { "Friday", 6 },
                    { "Saturday", 10 },
                    { "Sunday", 10 }
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
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 10 },
                    { "Tuesday", 10 },
                    { "Wednesday", 10 },
                    { "Thursday", 8 },
                    { "Friday", 10 },
                    { "Saturday", 0 },
                    { "Sunday", 0 }
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

        var schedule4 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 6 },
                    { "Tuesday", 8 },
                    { "Wednesday", 6 },
                    { "Thursday", 0 },
                    { "Friday", 0 },
                    { "Saturday", 12 },
                    { "Sunday", 8 }
                },
            BasePay = 25,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "charlie",
                Password = Hasher.HashPassword("ChocolateFactory"),
                Email = "charlie@example.com",
                Schedule = new Schedule()
            }
        };
        schedule4.UserId = schedule4.User.Id;
        schedule4.RecalculateTotalHours();

        var schedule5 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 4 },
                    { "Tuesday", 4 },
                    { "Wednesday", 4 },
                    { "Thursday", 4 },
                    { "Friday", 4 },
                    { "Saturday", 4 },
                    { "Sunday", 4 }
                },
            BasePay = 18,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "diana",
                Password = Hasher.HashPassword("WonderWoman"),
                Email = "diana@example.com",
                Schedule = new Schedule()
            }
        };
        schedule5.UserId = schedule5.User.Id;
        schedule5.RecalculateTotalHours();

        var schedule6 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 12 },
                    { "Tuesday", 0 },
                    { "Wednesday", 12 },
                    { "Thursday", 0 },
                    { "Friday", 12 },
                    { "Saturday", 0 },
                    { "Sunday", 4 }
                },
            BasePay = 35,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "ethan",
                Password = Hasher.HashPassword("MissionImpossible"),
                Email = "ethan@example.com",
                Schedule = new Schedule()
            }
        };
        schedule6.UserId = schedule6.User.Id;
        schedule6.RecalculateTotalHours();

        var schedule7 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 6 },
                    { "Wednesday", 0 },
                    { "Thursday", 6 },
                    { "Friday", 0 },
                    { "Saturday", 6 },
                    { "Sunday", 6 }
                },
            BasePay = 30,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "fiona",
                Password = Hasher.HashPassword("ShrekIsLife"),
                Email = "fiona@example.com",
                Schedule = new Schedule()
            }
        };
        schedule7.UserId = schedule7.User.Id;
        schedule7.RecalculateTotalHours();

        var schedule8 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 8 },
                    { "Tuesday", 8 },
                    { "Wednesday", 0 },
                    { "Thursday", 8 },
                    { "Friday", 8 },
                    { "Saturday", 6 },
                    { "Sunday", 2 }
                },
            BasePay = 24,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "george",
                Password = Hasher.HashPassword("CuriousGeorge"),
                Email = "george@example.com",
                Schedule = new Schedule()
            }
        };
        schedule8.UserId = schedule8.User.Id;
        schedule8.RecalculateTotalHours();

        var schedule9 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 5 },
                    { "Tuesday", 5 },
                    { "Wednesday", 5 },
                    { "Thursday", 5 },
                    { "Friday", 5 },
                    { "Saturday", 0 },
                    { "Sunday", 0 }
                },
            BasePay = 26,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "hannah",
                Password = Hasher.HashPassword("Montana123"),
                Email = "hannah@example.com",
                Schedule = new Schedule()
            }
        };
        schedule9.UserId = schedule9.User.Id;
        schedule9.RecalculateTotalHours();

        var schedule10 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 0 },
                    { "Wednesday", 0 },
                    { "Thursday", 14 },
                    { "Friday", 14 },
                    { "Saturday", 12 },
                    { "Sunday", 0 }
                },
            BasePay = 32,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "ivan",
                Password = Hasher.HashPassword("TerribleTsar"),
                Email = "ivan@example.com",
                Schedule = new Schedule()
            }
        };
        schedule10.UserId = schedule10.User.Id;
        schedule10.RecalculateTotalHours();

        var schedule11 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 3 },
                    { "Tuesday", 3 },
                    { "Wednesday", 3 },
                    { "Thursday", 3 },
                    { "Friday", 3 },
                    { "Saturday", 3 },
                    { "Sunday", 3 }
                },
            BasePay = 15,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "julia",
                Password = Hasher.HashPassword("CookingMaster"),
                Email = "julia@example.com",
                Schedule = new Schedule()
            }
        };
        schedule11.UserId = schedule11.User.Id;
        schedule11.RecalculateTotalHours();

        var schedule12 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 9 },
                    { "Tuesday", 9 },
                    { "Wednesday", 9 },
                    { "Thursday", 9 },
                    { "Friday", 0 },
                    { "Saturday", 0 },
                    { "Sunday", 4 }
                },
            BasePay = 27,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "kevin",
                Password = Hasher.HashPassword("HomeAlone"),
                Email = "kevin@example.com",
                Schedule = new Schedule()
            }
        };
        schedule12.UserId = schedule12.User.Id;
        schedule12.RecalculateTotalHours();

        var schedule13 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 6 },
                    { "Tuesday", 6 },
                    { "Wednesday", 6 },
                    { "Thursday", 6 },
                    { "Friday", 6 },
                    { "Saturday", 8 },
                    { "Sunday", 0 }
                },
            BasePay = 21,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "luna",
                Password = Hasher.HashPassword("Lovegood"),
                Email = "luna@example.com",
                Schedule = new Schedule()
            }
        };
        schedule13.UserId = schedule13.User.Id;
        schedule13.RecalculateTotalHours();

        var schedule14 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 12 },
                    { "Wednesday", 0 },
                    { "Thursday", 12 },
                    { "Friday", 0 },
                    { "Saturday", 12 },
                    { "Sunday", 0 }
                },
            BasePay = 40,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "marcus",
                Password = Hasher.HashPassword("Gladiator"),
                Email = "marcus@example.com",
                Schedule = new Schedule()
            }
        };
        schedule14.UserId = schedule14.User.Id;
        schedule14.RecalculateTotalHours();

        var schedule15 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 7 },
                    { "Tuesday", 7 },
                    { "Wednesday", 7 },
                    { "Thursday", 7 },
                    { "Friday", 7 },
                    { "Saturday", 5 },
                    { "Sunday", 5 }
                },
            BasePay = 23,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "nina",
                Password = Hasher.HashPassword("BlackSwan"),
                Email = "nina@example.com",
                Schedule = new Schedule()
            }
        };
        schedule15.UserId = schedule15.User.Id;
        schedule15.RecalculateTotalHours();

        var schedule16 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 2 },
                    { "Tuesday", 4 },
                    { "Wednesday", 6 },
                    { "Thursday", 8 },
                    { "Friday", 10 },
                    { "Saturday", 0 },
                    { "Sunday", 0 }
                },
            BasePay = 29,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "oscar",
                Password = Hasher.HashPassword("GrouchyBin"),
                Email = "oscar@example.com",
                Schedule = new Schedule()
            }
        };
        schedule16.UserId = schedule16.User.Id;
        schedule16.RecalculateTotalHours();

        var schedule17 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 0 },
                    { "Wednesday", 8 },
                    { "Thursday", 8 },
                    { "Friday", 8 },
                    { "Saturday", 8 },
                    { "Sunday", 8 }
                },
            BasePay = 33,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "penny",
                Password = Hasher.HashPassword("BigBangTheory"),
                Email = "penny@example.com",
                Schedule = new Schedule()
            }
        };
        schedule17.UserId = schedule17.User.Id;
        schedule17.RecalculateTotalHours();

        var schedule18 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 1 },
                    { "Tuesday", 2 },
                    { "Wednesday", 3 },
                    { "Thursday", 4 },
                    { "Friday", 5 },
                    { "Saturday", 6 },
                    { "Sunday", 7 }
                },
            BasePay = 16,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "quinn",
                Password = Hasher.HashPassword("HarleyQuinn"),
                Email = "quinn@example.com",
                Schedule = new Schedule()
            }
        };
        schedule18.UserId = schedule18.User.Id;
        schedule18.RecalculateTotalHours();

        var schedule19 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 11 },
                    { "Tuesday", 11 },
                    { "Wednesday", 0 },
                    { "Thursday", 0 },
                    { "Friday", 11 },
                    { "Saturday", 7 },
                    { "Sunday", 0 }
                },
            BasePay = 31,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "riley",
                Password = Hasher.HashPassword("InsideOut"),
                Email = "riley@example.com",
                Schedule = new Schedule()
            }
        };
        schedule19.UserId = schedule19.User.Id;
        schedule19.RecalculateTotalHours();

        var schedule20 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked =
                new Dictionary<string, int>
                {
                    { "Monday", 0 },
                    { "Tuesday", 1 },
                    { "Wednesday", 2 },
                    { "Thursday", 3 },
                    { "Friday", 4 },
                    { "Saturday", 5 },
                    { "Sunday", 6 }
                },
            BasePay = 45,
            User = new User
            {
                Id = Guid.NewGuid(),
                Username = "sam",
                Password = Hasher.HashPassword("FrodoFriend"),
                Email = "sam@example.com",
                Schedule = new Schedule()
            }
        };
        schedule20.UserId = schedule20.User.Id;
        schedule20.RecalculateTotalHours();

        // Add all schedules to the list
        schedules.AddRange(new[]
        {
            schedule1, schedule2, schedule3, schedule4, schedule5, schedule6, schedule7, schedule8, schedule9,
            schedule10, schedule11, schedule12, schedule13, schedule14, schedule15, schedule16, schedule17,
            schedule18, schedule19, schedule20
        });

        await scheduleContext.Schedules.AddRangeAsync(schedules);
        await scheduleContext.SaveChangesAsync();
    }
}
