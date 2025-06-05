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

        // Standard 40-hour work week (no overtime)
        var schedule1 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 8 }, { "Wednesday", 8 },
                { "Thursday", 8 }, { "Friday", 8 }, { "Saturday", 0 }, { "Sunday", 0 }
            },
            BasePay = 25.00,
            User = CreateScheduleUser("standard.worker", "StandardWorker123!", "standard@company.com")
        };
        schedule1.UserId = schedule1.User.Id;
        schedule1.RecalculateTotalHours();

        // Heavy overtime - 60+ hours per week
        var schedule2 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 12 }, { "Tuesday", 12 }, { "Wednesday", 10 },
                { "Thursday", 12 }, { "Friday", 10 }, { "Saturday", 8 }, { "Sunday", 6 }
            },
            BasePay = 30.00,
            User = CreateScheduleUser("overtime.hero", "WorkHard247!", "overtime@company.com")
        };
        schedule2.UserId = schedule2.User.Id;
        schedule2.RecalculateTotalHours();

        // Weekend warrior - mostly weekend overtime
        var schedule3 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 8 }, { "Wednesday", 8 },
                { "Thursday", 8 }, { "Friday", 8 }, { "Saturday", 12 }, { "Sunday", 10 }
            },
            BasePay = 28.50,
            User = CreateScheduleUser("weekend.warrior", "SatSunWork!", "weekend@company.com")
        };
        schedule3.UserId = schedule3.User.Id;
        schedule3.RecalculateTotalHours();

        // Extreme overtime - 80+ hours per week
        var schedule4 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 14 }, { "Tuesday", 14 }, { "Wednesday", 12 },
                { "Thursday", 14 }, { "Friday", 12 }, { "Saturday", 10 }, { "Sunday", 8 }
            },
            BasePay = 35.00,
            User = CreateScheduleUser("workaholic.max", "NeverStop999!", "workaholic@company.com")
        };
        schedule4.UserId = schedule4.User.Id;
        schedule4.RecalculateTotalHours();

        // Part-time with occasional overtime
        var schedule5 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 6 }, { "Tuesday", 8 }, { "Wednesday", 10 },
                { "Thursday", 8 }, { "Friday", 12 }, { "Saturday", 0 }, { "Sunday", 0 }
            },
            BasePay = 22.75,
            User = CreateScheduleUser("part.timer", "PartTime456!", "parttime@company.com")
        };
        schedule5.UserId = schedule5.User.Id;
        schedule5.RecalculateTotalHours();

        // Shift worker with overtime
        var schedule6 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 12 }, { "Tuesday", 0 }, { "Wednesday", 12 },
                { "Thursday", 0 }, { "Friday", 12 }, { "Saturday", 8 }, { "Sunday", 8 }
            },
            BasePay = 32.25,
            User = CreateScheduleUser("shift.worker", "ShiftLife789!", "shift@company.com")
        };
        schedule6.UserId = schedule6.User.Id;
        schedule6.RecalculateTotalHours();

        // Healthcare worker - long shifts with overtime
        var schedule7 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 12 }, { "Tuesday", 12 }, { "Wednesday", 0 },
                { "Thursday", 12 }, { "Friday", 12 }, { "Saturday", 8 }, { "Sunday", 0 }
            },
            BasePay = 38.50,
            User = CreateScheduleUser("nurse.dedicated", "HealthCare123!", "nurse@hospital.com")
        };
        schedule7.UserId = schedule7.User.Id;
        schedule7.RecalculateTotalHours();

        // On-call technician - irregular overtime
        var schedule8 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 10 }, { "Wednesday", 6 },
                { "Thursday", 14 }, { "Friday", 8 }, { "Saturday", 4 }, { "Sunday", 12 }
            },
            BasePay = 29.00,
            User = CreateScheduleUser("tech.oncall", "OnCall247!", "oncall@tech.com")
        };
        schedule8.UserId = schedule8.User.Id;
        schedule8.RecalculateTotalHours();

        // Construction worker - seasonal overtime
        var schedule9 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 }, { "Tuesday", 10 }, { "Wednesday", 10 },
                { "Thursday", 10 }, { "Friday", 10 }, { "Saturday", 8 }, { "Sunday", 4 }
            },
            BasePay = 31.75,
            User = CreateScheduleUser("construction.pro", "BuildStrong!", "builder@construction.com")
        };
        schedule9.UserId = schedule9.User.Id;
        schedule9.RecalculateTotalHours();

        // Restaurant manager - service industry overtime
        var schedule10 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 9 }, { "Wednesday", 11 },
                { "Thursday", 12 }, { "Friday", 13 }, { "Saturday", 12 }, { "Sunday", 10 }
            },
            BasePay = 26.50,
            User = CreateScheduleUser("restaurant.mgr", "ServiceFirst!", "manager@restaurant.com")
        };
        schedule10.UserId = schedule10.User.Id;
        schedule10.RecalculateTotalHours();

        // Retail supervisor - holiday overtime
        var schedule11 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 9 }, { "Tuesday", 8 }, { "Wednesday", 10 },
                { "Thursday", 11 }, { "Friday", 12 }, { "Saturday", 10 }, { "Sunday", 8 }
            },
            BasePay = 24.25,
            User = CreateScheduleUser("retail.super", "RetailLife!", "supervisor@retail.com")
        };
        schedule11.UserId = schedule11.User.Id;
        schedule11.RecalculateTotalHours();

        // Security guard - double shifts
        var schedule12 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 16 }, { "Tuesday", 0 }, { "Wednesday", 16 },
                { "Thursday", 0 }, { "Friday", 16 }, { "Saturday", 8 }, { "Sunday", 8 }
            },
            BasePay = 27.00,
            User = CreateScheduleUser("security.guard", "NightWatch!", "security@company.com")
        };
        schedule12.UserId = schedule12.User.Id;
        schedule12.RecalculateTotalHours();

        // Delivery driver - peak season overtime
        var schedule13 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 9 }, { "Tuesday", 10 }, { "Wednesday", 8 },
                { "Thursday", 11 }, { "Friday", 12 }, { "Saturday", 10 }, { "Sunday", 6 }
            },
            BasePay = 23.50,
            User = CreateScheduleUser("delivery.driver", "FastDelivery!", "driver@logistics.com")
        };
        schedule13.UserId = schedule13.User.Id;
        schedule13.RecalculateTotalHours();

        // Factory worker - mandatory overtime
        var schedule14 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 }, { "Tuesday", 10 }, { "Wednesday", 10 },
                { "Thursday", 10 }, { "Friday", 10 }, { "Saturday", 6 }, { "Sunday", 0 }
            },
            BasePay = 28.75,
            User = CreateScheduleUser("factory.worker", "Production!", "worker@factory.com")
        };
        schedule14.UserId = schedule14.User.Id;
        schedule14.RecalculateTotalHours();

        // IT support - emergency overtime
        var schedule15 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 }, { "Tuesday", 12 }, { "Wednesday", 8 },
                { "Thursday", 16 }, { "Friday", 10 }, { "Saturday", 0 }, { "Sunday", 6 }
            },
            BasePay = 33.00,
            User = CreateScheduleUser("it.support", "TechSupport!", "support@tech.com")
        };
        schedule15.UserId = schedule15.User.Id;
        schedule15.RecalculateTotalHours();

        // Minimal hours - no overtime
        var schedule16 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 4 }, { "Tuesday", 4 }, { "Wednesday", 4 },
                { "Thursday", 4 }, { "Friday", 4 }, { "Saturday", 0 }, { "Sunday", 0 }
            },
            BasePay = 18.00,
            User = CreateScheduleUser("part.student", "Student123!", "student@university.edu")
        };
        schedule16.UserId = schedule16.User.Id;
        schedule16.RecalculateTotalHours();

        // Consultant - project crunch overtime
        var schedule17 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 12 }, { "Tuesday", 14 }, { "Wednesday", 10 },
                { "Thursday", 12 }, { "Friday", 16 }, { "Saturday", 8 }, { "Sunday", 4 }
            },
            BasePay = 45.00,
            User = CreateScheduleUser("consultant.pro", "ProjectCrunch!", "consultant@firm.com")
        };
        schedule17.UserId = schedule17.User.Id;
        schedule17.RecalculateTotalHours();

        // Airline pilot - irregular but high overtime
        var schedule18 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 0 }, { "Tuesday", 14 }, { "Wednesday", 0 },
                { "Thursday", 12 }, { "Friday", 0 }, { "Saturday", 16 }, { "Sunday", 8 }
            },
            BasePay = 52.00,
            User = CreateScheduleUser("pilot.captain", "FlyHigh!", "pilot@airline.com")
        };
        schedule18.UserId = schedule18.User.Id;
        schedule18.RecalculateTotalHours();

        // Freelancer - project-based overtime
        var schedule19 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 6 }, { "Tuesday", 8 }, { "Wednesday", 12 },
                { "Thursday", 10 }, { "Friday", 14 }, { "Saturday", 6 }, { "Sunday", 8 }
            },
            BasePay = 40.00,
            User = CreateScheduleUser("freelancer.dev", "CodeAllNight!", "freelancer@dev.com")
        };
        schedule19.UserId = schedule19.User.Id;
        schedule19.RecalculateTotalHours();

        // Emergency responder - crisis overtime
        var schedule20 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 12 }, { "Tuesday", 8 }, { "Wednesday", 16 },
                { "Thursday", 12 }, { "Friday", 10 }, { "Saturday", 12 }, { "Sunday", 8 }
            },
            BasePay = 36.50,
            User = CreateScheduleUser("emergency.responder", "FirstResponder!", "responder@emergency.gov")
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

        Console.WriteLine($"Seeded {schedules.Count} schedules with varied overtime patterns");
    }

    private static User CreateScheduleUser(string username, string password, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Password = Hasher.HashPassword(password),
            Email = email,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
            UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
            Schedule = new Schedule()
        };
    }
}
