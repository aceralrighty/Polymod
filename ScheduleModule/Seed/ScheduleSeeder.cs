using Microsoft.EntityFrameworkCore;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;
using TBD.MetricsModule.Services;

namespace TBD.ScheduleModule.Seed;

public static class ScheduleSeeder
{
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var scheduleContext = scope.ServiceProvider.GetRequiredService<ScheduleDbContext>();
        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var metricsService = scope.ServiceProvider.GetRequiredService<IMetricsService>();

        metricsService.IncrementCounter("seeding.schedule_reseed_started");

        await userContext.Database.EnsureDeletedAsync();
        await scheduleContext.Database.EnsureDeletedAsync();
        await userContext.Database.MigrateAsync();
        await scheduleContext.Database.MigrateAsync();

        metricsService.IncrementCounter("seeding.schedule_database_recreated");

        await SeedScheduleAsync(scheduleContext, metricsService);

        metricsService.IncrementCounter("seeding.schedule_reseed_completed");
    }

    private static async Task SeedScheduleAsync(ScheduleDbContext scheduleContext, IMetricsService metricsService)
    {
        metricsService.IncrementCounter("seeding.schedule_seed_started");

        var schedules = new List<Schedule>();

        // Standard 40-hour work week (no overtime)
        // Add these additional test cases to your existing seed data for comprehensive edge case testing

        // EXACT THRESHOLD TESTS - These are critical for testing boundary conditions

        // Exactly 40 hours - no overtime (boundary test)
        var scheduleExact40 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            },
            BasePay = 25.00,
            User = CreateScheduleUser("exact.forty", "Exactly40!", "exact40@test.com")
        };

        // Exactly 40.5 hours - minimal overtime (just over threshold)
        var scheduleJustOver40 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 1 } // 41 hours total
            },
            BasePay = 30.00,
            User = CreateScheduleUser("just.over.forty", "JustOver40!", "justover40@test.com")
        };

        // Exactly 60 hours - maximum 1.5x overtime, no 2x yet (critical boundary)
        var scheduleExact60 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 0 }
            },
            BasePay = 35.00,
            User = CreateScheduleUser("exact.sixty", "Exactly60!", "exact60@test.com")
        };

        // Exactly 61 hours - minimal 2x overtime (just over 60 threshold)
        var scheduleJustOver60 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 1 }
            },
            BasePay = 32.00,
            User = CreateScheduleUser("just.over.sixty", "JustOver60!", "justover60@test.com")
        };

        // EXTREME CASES

        // Zero hours worked
        var scheduleZero = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            },
            BasePay = 20.00,
            User = CreateScheduleUser("zero.hours", "NoWork123!", "zero@test.com")
        };

        // Single-hour worked
        var scheduleOne = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 1 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            },
            BasePay = 15.00,
            User = CreateScheduleUser("one.hour", "SingleHour!", "onehour@test.com")
        };

        // Massive overtime - 100+ hours (stress test)
        var scheduleMassive = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 16 },
                { "Tuesday", 16 },
                { "Wednesday", 16 },
                { "Thursday", 16 },
                { "Friday", 16 },
                { "Saturday", 16 },
                { "Sunday", 16 }
            }, // 112 hours total
            BasePay = 25.00,
            User = CreateScheduleUser("workaholic.extreme", "Work247365!", "extreme@test.com")
        };

        // FRACTIONAL BOUNDARY TESTS (if your system supports fractional hours)

        // 39.5 hours - just under 40
        var scheduleJustUnder40 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 7 },
                { "Saturday", 0 },
                { "Sunday", 1 } // 40 hours (or use 7.5 + 0.5 if you support decimals)
            },
            BasePay = 22.50,
            User = CreateScheduleUser("just.under.forty", "AlmostForty!", "under40@test.com")
        };

        // 59.5 hours - just under 60
        var scheduleJustUnder60 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 9 },
                { "Wednesday", 9 },
                { "Thursday", 9 },
                { "Friday", 9 },
                { "Saturday", 9 },
                { "Sunday", 5 }
            }, // 59 hours
            BasePay = 28.00,
            User = CreateScheduleUser("just.under.sixty", "AlmostSixty!", "under60@test.com")
        };

        // HIGH-VALUE EDGE CASES

        // High pay rate with exact threshold hours
        var scheduleHighPayExact60 = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 0 }
            },
            BasePay = 75.00, // High rate to test large dollar calculations
            User = CreateScheduleUser("executive.sixty", "HighPay60!", "exec60@test.com")
        };

        // Low pay rate with massive overtime
        var scheduleLowPayMassive = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 14 },
                { "Tuesday", 14 },
                { "Wednesday", 14 },
                { "Thursday", 14 },
                { "Friday", 14 },
                { "Saturday", 12 },
                { "Sunday", 8 }
            }, // 90 hours
            BasePay = 12.50, // Minimum wage scenario
            User = CreateScheduleUser("minimum.wage.hero", "WorkHard!", "minwage@test.com")
        };

        // UNUSUAL PATTERNS

        // All hours on one day (edge case for daily limits if you add them later)
        var scheduleOneDay = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 65 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            },
            BasePay = 30.00,
            User = CreateScheduleUser("marathon.worker", "OneDay65!", "marathon@test.com")
        };

        // Perfect distribution across all tiers
        var schedulePerfectTiers = new Schedule
        {
            Id = Guid.NewGuid(),
            DaysWorked = new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 10 },
                { "Sunday", 12 }
            }, // 62 hours: 40 regular + 20 at 1.5x + 2 at 2x
            BasePay = 25.00,
            User = CreateScheduleUser("perfect.tiers", "AllTiers!", "tiers@test.com")
        };

        schedules.AddRange([
            scheduleExact40, scheduleJustOver40, scheduleExact60, scheduleJustOver60, scheduleZero, scheduleOne,
            scheduleMassive, scheduleJustUnder40, scheduleJustUnder60, scheduleHighPayExact60,
            scheduleLowPayMassive, scheduleOneDay, schedulePerfectTiers
        ]);

        // I'm actually prouder of this one line than I should be.
        foreach (var calc in schedules)
        {
            calc.RecalculateTotalHours();
        }

        // Track schedules by various categories
        var regularTimeSchedules = schedules.Count(s => GetTotalHours(s) <= 40);
        var overtimeSchedules = schedules.Count(s => GetTotalHours(s) > 40 && GetTotalHours(s) <= 60);
        var doubleOvertimeSchedules = schedules.Count(s => GetTotalHours(s) > 60);
        var zeroHourSchedules = schedules.Count(s => GetTotalHours(s) == 0);
        var extremeHourSchedules = schedules.Count(s => GetTotalHours(s) > 80);
        var highPaySchedules = schedules.Count(s => s.BasePay > 50.00);
        var lowPaySchedules = schedules.Count(s => s.BasePay < 20.00);
        var boundarySchedules = schedules.Count(s => IsBoundarySchedule(s));
        var weekendWorkSchedules = schedules.Count(s => HasWeekendWork(s));
        var singleDaySchedules = schedules.Count(s => IsSingleDaySchedule(s));

        // Log total schedules created
        for (int i = 0; i < schedules.Count; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_total");
        }

        // Log hour-based categories
        for (int i = 0; i < regularTimeSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_regular_time");
        }

        for (int i = 0; i < overtimeSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_overtime");
        }

        for (int i = 0; i < doubleOvertimeSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_double_overtime");
        }

        for (int i = 0; i < zeroHourSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_zero_hours");
        }

        for (int i = 0; i < extremeHourSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_extreme_hours");
        }

        // Log pay-based categories
        for (int i = 0; i < highPaySchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_high_pay");
        }

        for (int i = 0; i < lowPaySchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_low_pay");
        }

        // Log pattern-based categories
        for (int i = 0; i < boundarySchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_boundary_test");
        }

        for (int i = 0; i < weekendWorkSchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_weekend_work");
        }

        for (int i = 0; i < singleDaySchedules; i++)
        {
            metricsService.IncrementCounter("seeding.schedules_created_single_day");
        }

        await scheduleContext.Schedules.AddRangeAsync(schedules);
        await scheduleContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.schedule_database_save_completed");
        metricsService.IncrementCounter("seeding.schedule_seed_completed");

        Console.WriteLine($"Seeded {schedules.Count} schedules with varied overtime patterns");
    }

    private static int GetTotalHours(Schedule schedule)
    {
        return schedule.DaysWorked.Values.Sum();
    }

    private static bool IsBoundarySchedule(Schedule schedule)
    {
        var totalHours = GetTotalHours(schedule);
        return totalHours is 40 or 41 or 60 or 61;
    }

    private static bool HasWeekendWork(Schedule schedule)
    {
        return schedule.DaysWorked.GetValueOrDefault("Saturday", 0) > 0 ||
               schedule.DaysWorked.GetValueOrDefault("Sunday", 0) > 0;
    }

    private static bool IsSingleDaySchedule(Schedule schedule)
    {
        var daysWithWork = schedule.DaysWorked.Values.Count(hours => hours > 0);
        return daysWithWork == 1;
    }

    private static User CreateScheduleUser(string username, string password, string email)
    {
        var hasher = new Hasher();
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Password = hasher.HashPassword(password),
            Email = email,
            CreatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(30, 365)),
            UpdatedAt = DateTime.UtcNow.AddDays(-Random.Shared.Next(1, 30)),
            Schedule = new Schedule()
        };
    }
}
