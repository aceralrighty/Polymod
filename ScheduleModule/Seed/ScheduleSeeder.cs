using System.Diagnostics;
using Bogus;
using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.Users;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.Shared.Events.Interfaces;

// Import Bogus

// Needed for explicit JSON handling if not using the property setter

namespace TBD.ScheduleModule.Seed;

public static class ScheduleSeeder
{
    private static readonly Random Random = new();
    private static readonly ActivitySource ActivitySource = new("TBD.ScheduleModule.ScheduleSeeder");

    private static readonly string[] StringArray =
    [
        "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"
    ];

    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider,
        int numberOfAdditionalRandomSchedules = 50)
    {
        using var activity = ActivitySource.StartActivity("ReseedForTesting");
        activity?.SetTag("operation", "ReseedForTesting");
        activity?.SetTag("number_of_additional_random_schedules", numberOfAdditionalRandomSchedules);

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ScheduleDbContext>();
        var userService = scope.ServiceProvider.GetRequiredService<IUserReadService>();

        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("ScheduleModule");

        metricsService.IncrementCounter("seeding.schedule_reseed_started");

        Console.WriteLine("🗑️ Deleting existing schedule database...");
        await db.Database.EnsureDeletedAsync();
        metricsService.IncrementCounter("seeding.schedule_db_delete_completed");
        Console.WriteLine("📊 Migrating schedule database...");
        await db.Database.MigrateAsync();
        metricsService.IncrementCounter("seeding.schedule_db_migration_completed");

        var users = await userService.GetAllUsersAsync();
        Console.WriteLine($"👥 Found {users.Count} users from UserModule.");

        // We only create one schedule per user initially, if none exist.
        // The detailed seeding logic in SeedScheduleAsync will handle generating more.
        if (users.Count != 0 && !await db.Schedules.AnyAsync())
        {
            Console.WriteLine("Creating initial empty schedules for users...");
            foreach (var user in users.OfType<UserDto>())
            {
                // Set DaysWorked dictionary, which will populate DaysWorkedJson
                var newSchedule = new Schedule
                {
                    Id = Guid.NewGuid(), UserId = user.Id, DaysWorked = new Dictionary<string, int>(), BasePay = 0.0f
                };
                newSchedule.RecalculateTotalHours(); // Recalculate after setting DaysWorked
                db.Schedules.Add(newSchedule);
            }

            await db.SaveChangesAsync();
            Console.WriteLine($"Created {users.Count} initial empty schedules.");
        }

        metricsService.IncrementCounter("seeding.schedule_database_recreated");

        // Pass a numberOfAdditionalRandomSchedules to the seeding method
        await SeedScheduleAsync(db, metricsService, users.OfType<UserDto>().ToList(),
            numberOfAdditionalRandomSchedules);

        metricsService.IncrementCounter("seeding.schedule_reseed_completed");
        Console.WriteLine("✅ Schedule reseed completed.");
        await db.Schedules.ToListAsync();
    }

    private static async Task SeedScheduleAsync(ScheduleDbContext scheduleContext, IMetricsService metricsService,
        List<UserDto> users, int numberOfAdditionalRandomSchedules)
    {
        using var activity = ActivitySource.StartActivity();
        activity?.SetTag("step", "seed_schedules");
        activity?.SetTag("number_of_additional_random_schedules", numberOfAdditionalRandomSchedules);

        metricsService.IncrementCounter("seeding.schedule_seed_started");
        Console.WriteLine("🌱 Starting schedule seeding...");

        // If no users exist, we can't seed schedules for them.
        if (users.Count == 0)
        {
            Console.WriteLine("❌ No users found. Skipping schedule seeding.");
            activity?.SetTag("skipped_no_users", true);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        // Get existing schedules and mark users that already have one.
        var existingSchedules = await scheduleContext.Schedules.ToListAsync();
        var usedUserIds = new HashSet<Guid>(existingSchedules.Select(s => s.UserId));
        var availableUsers = users.Where(u => !usedUserIds.Contains(u.Id)).ToList();

        var schedulesToSeed = new List<Schedule>();

        // 1. Add the fixed test schedule data first
        var testScheduleData = GetTestScheduleData();
        Console.WriteLine($"Adding {testScheduleData.Count} fixed test schedules.");

        foreach (var data in testScheduleData)
        {
            if (availableUsers.Count != 0)
            {
                var user = availableUsers[Random.Next(availableUsers.Count)];
                availableUsers.Remove(user); // Ensure unique user assignment for test schedules

                var newSchedule = new Schedule
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    DaysWorked = data.DaysWorked, // Set the Dictionary property
                    BasePay = data.BasePay
                };
                newSchedule.RecalculateTotalHours(); // Ensure TotalHoursWorked is calculated
                schedulesToSeed.Add(newSchedule);
            }
            else
            {
                Console.WriteLine("⚠️ Not enough unique users for all fixed test schedules. Skipping some.");
                break; // Stop adding fixed schedules if no more unique users
            }
        }

        switch (numberOfAdditionalRandomSchedules)
        {
            // 2. Generate additional random schedules using Bogus
            case > 0 when availableUsers.Count != 0:
            {
                Console.WriteLine($"Generating {numberOfAdditionalRandomSchedules} additional random schedules.");

                // Create a Faker for the Schedule model
                var scheduleFaker = new Faker<Schedule>()
                    .RuleFor(s => s.Id, _ => Guid.NewGuid())
                    .RuleFor(s => s.UserId, f => f.PickRandom(users.Select(u => u.Id).ToArray()))
                    .RuleFor(s => s.DaysWorked, f =>
                    {
                        var daysWorked = new Dictionary<string, int>();
                        var totalHours = f.Random.Int(0, 80); // Total hours between 0 and 80 for random schedules
                        var remainingHours = totalHours;
                        var days = StringArray;

                        // Distribute hours somewhat randomly across days
                        foreach (var day in days.OrderBy(_ => f.Random.Guid())) // Randomize day order
                        {
                            if (remainingHours <= 0) break;

                            var hoursToday = f.Random.Int(0, Math.Min(remainingHours, 14)); // Max 14 hours per day
                            daysWorked[day] = hoursToday;
                            remainingHours -= hoursToday;
                        }

                        // Ensure all days are present, even if 0 hours
                        foreach (var day in days)
                        {
                            daysWorked.TryAdd(day, 0);
                        }

                        // If any remaining hours, add them to a random day
                        if (remainingHours <= 0)
                        {
                            return daysWorked;
                        }

                        var dayToAddTo = days[f.Random.Int(0, days.Length - 1)];
                        daysWorked[dayToAddTo] += remainingHours;

                        return daysWorked;
                    })
                    .RuleFor(s => s.BasePay,
                        f => (float)Math.Round(f.Random.Double(12.00, 75.00), 2)); // Random pay between $12-$75

                var newRandomSchedules =
                    scheduleFaker.Generate(Math.Min(numberOfAdditionalRandomSchedules, availableUsers.Count));

                foreach (var schedule in newRandomSchedules)
                {
                    var user = availableUsers[Random.Next(availableUsers.Count)];
                    availableUsers.Remove(user);
                    schedule.UserId = user.Id;
                    schedule.RecalculateTotalHours(); // Recalculate TotalHoursWorked after DaysWorked is set by Faker
                    schedulesToSeed.Add(schedule);
                }

                break;
            }
            case > 0 when !availableUsers.Any():
                Console.WriteLine("⚠️ No more available users to create additional random schedules.");
                break;
        }

        // Add schedules to context and save
        if (schedulesToSeed.Count != 0)
        {
            Console.WriteLine($"Saving {schedulesToSeed.Count} schedules to the database.");
            await scheduleContext.Schedules.AddRangeAsync(schedulesToSeed);
            await scheduleContext.SaveChangesAsync();
            metricsService.IncrementCounter("seeding.schedule_database_save_completed");
            Console.WriteLine($"✅ Seeded {schedulesToSeed.Count} new schedules.");

            // Log metrics based on the newly seeded schedules
            LogScheduleMetrics(schedulesToSeed, metricsService, activity);
        }
        else
        {
            Console.WriteLine("🤷 No new schedules to seed.");
        }

        metricsService.IncrementCounter("seeding.schedule_seed_completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    // Helper to log metrics for a list of schedules
    private static void LogScheduleMetrics(List<Schedule> schedules, IMetricsService metricsService, Activity? activity)
    {
        var regularTimeSchedules = schedules.Count(s => GetTotalHours(s) <= 40);
        var overtimeSchedules = schedules.Count(s => GetTotalHours(s) > 40 && GetTotalHours(s) <= 60);
        var doubleOvertimeSchedules = schedules.Count(s => GetTotalHours(s) > 60);
        var zeroHourSchedules = schedules.Count(s => GetTotalHours(s) == 0);
        var extremeHourSchedules = schedules.Count(s => GetTotalHours(s) > 80);
        var highPaySchedules = schedules.Count(s => s.BasePay > 50.00f); // Note: Used float? in model
        var lowPaySchedules = schedules.Count(s => s.BasePay < 20.00f); // Note: Used float? in model
        var boundarySchedules = schedules.Count(IsBoundarySchedule);
        var weekendWorkSchedules = schedules.Count(HasWeekendWork);
        var singleDaySchedules = schedules.Count(IsSingleDaySchedule);

        activity?.SetTag("seeded_schedules_total", schedules.Count);
        activity?.SetTag("seeded_schedules_regular_time", regularTimeSchedules);
        activity?.SetTag("seeded_schedules_overtime", overtimeSchedules);
        activity?.SetTag("seeded_schedules_double_overtime", doubleOvertimeSchedules);
        activity?.SetTag("seeded_schedules_zero_hours", zeroHourSchedules);
        activity?.SetTag("seeded_schedules_extreme_hours", extremeHourSchedules);
        activity?.SetTag("seeded_schedules_high_pay", highPaySchedules);
        activity?.SetTag("seeded_schedules_low_pay", lowPaySchedules);
        activity?.SetTag("seeded_schedules_boundary_test", boundarySchedules);
        activity?.SetTag("seeded_schedules_weekend_work", weekendWorkSchedules);
        activity?.SetTag("seeded_schedules_single_day", singleDaySchedules);

        metricsService.IncrementCounter($"seeding.schedules_created_total -> {schedules.Count}");
        metricsService.IncrementCounter($"seeding.schedules_created_regular_time -> {regularTimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_overtime -> {overtimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_double_overtime -> {doubleOvertimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_zero_hours -> {zeroHourSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_extreme_hours -> {extremeHourSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_high_pay -> {highPaySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_low_pay -> {lowPaySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_boundary_test -> {boundarySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_weekend_work -> {weekendWorkSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_single_day -> {singleDaySchedules}");
    }


    private static List<(Dictionary<string, int> DaysWorked, float BasePay)> GetTestScheduleData()
    {
        return
        [
            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 25.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 1 }
            }, 30.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 0 }
            }, 35.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 1 }
            }, 32.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 20.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 1 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 15.00f),

            // REALISTIC WORK PATTERNS

            (new Dictionary<string, int>
            {
                { "Monday", 7 },
                { "Tuesday", 8 },
                { "Wednesday", 7 },
                { "Thursday", 8 },
                { "Friday", 7 },
                { "Saturday", 0 },
                { "Sunday", 1 }
            }, 28.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 4 },
                { "Wednesday", 6 },
                { "Thursday", 4 },
                { "Friday", 8 },
                { "Saturday", 8 },
                { "Sunday", 6 }
            }, 16.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 5 },
                { "Wednesday", 6 },
                { "Thursday", 7 },
                { "Friday", 10 },
                { "Saturday", 12 },
                { "Sunday", 11 }
            }, 18.25f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 0 },
                { "Wednesday", 12 },
                { "Thursday", 0 },
                { "Friday", 12 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 42.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 12 },
                { "Wednesday", 10 },
                { "Thursday", 12 },
                { "Friday", 10 },
                { "Saturday", 8 },
                { "Sunday", 4 }
            }, 33.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 9 },
                { "Wednesday", 8 },
                { "Thursday", 10 },
                { "Friday", 9 },
                { "Saturday", 0 },
                { "Sunday", 2 }
            }, 38.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 3 },
                { "Wednesday", 0 },
                { "Thursday", 4 },
                { "Friday", 0 },
                { "Saturday", 8 },
                { "Sunday", 5 }
            }, 14.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 2 },
                { "Wednesday", 8 },
                { "Thursday", 0 },
                { "Friday", 14 },
                { "Saturday", 6 },
                { "Sunday", 3 }
            }, 65.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 31.25f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 11 },
                { "Wednesday", 9 },
                { "Thursday", 12 },
                { "Friday", 10 },
                { "Saturday", 3 },
                { "Sunday", 5 }
            }, 55.00f),

            // EDGE CASES AND STRESS TESTS

            (new Dictionary<string, int>
            {
                { "Monday", 16 },
                { "Tuesday", 16 },
                { "Wednesday", 16 },
                { "Thursday", 16 },
                { "Friday", 16 },
                { "Saturday", 16 },
                { "Sunday", 16 }
            }, 25.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 20 },
                { "Sunday", 18 }
            }, 22.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 16 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 16 },
                { "Saturday", 4 },
                { "Sunday", 0 }
            }, 27.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 10 },
                { "Wednesday", 9 },
                { "Thursday", 8 },
                { "Friday", 7 },
                { "Saturday", 2 },
                { "Sunday", 3 }
            }, 125.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 8 },
                { "Sunday", 6 }
            }, 12.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 3 },
                { "Tuesday", 0 },
                { "Wednesday", 12 },
                { "Thursday", 1 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 9 }
            }, 19.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 12 },
                { "Wednesday", 10 },
                { "Thursday", 12 },
                { "Friday", 14 },
                { "Saturday", 12 },
                { "Sunday", 8 }
            }, 24.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 4 },
                { "Tuesday", 6 },
                { "Wednesday", 4 },
                { "Thursday", 6 },
                { "Friday", 3 },
                { "Saturday", 8 },
                { "Sunday", 2 }
            }, 35.00f),

            // ADDITIONAL REALISTIC SCENARIOS

            (new Dictionary<string, int>
            {
                { "Monday", 6 },
                { "Tuesday", 6 },
                { "Wednesday", 6 },
                { "Thursday", 6 },
                { "Friday", 6 },
                { "Saturday", 4 },
                { "Sunday", 0 }
            }, 21.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 8 },
                { "Wednesday", 10 },
                { "Thursday", 9 },
                { "Friday", 8 },
                { "Saturday", 2 },
                { "Sunday", 4 }
            }, 47.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 12 },
                { "Wednesday", 0 },
                { "Thursday", 12 },
                { "Friday", 12 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 19.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 7 },
                { "Wednesday", 8 },
                { "Thursday", 9 },
                { "Friday", 10 },
                { "Saturday", 6 },
                { "Sunday", 4 }
            }, 17.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 7 },
                { "Tuesday", 9 },
                { "Wednesday", 8 },
                { "Thursday", 6 },
                { "Friday", 10 },
                { "Saturday", 0 },
                { "Sunday", 5 }
            }, 78.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 8 },
                { "Saturday", 8 },
                { "Sunday", 6 }
            }, 16.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 8 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 23.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 5 },
                { "Tuesday", 6 },
                { "Wednesday", 0 },
                { "Thursday", 7 },
                { "Friday", 8 },
                { "Saturday", 8 },
                { "Sunday", 6 }
            }, 15.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 0 },
                { "Wednesday", 12 },
                { "Thursday", 0 },
                { "Friday", 12 },
                { "Saturday", 12 },
                { "Sunday", 0 }
            }, 38.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 6 },
                { "Wednesday", 4 },
                { "Thursday", 8 },
                { "Friday", 12 },
                { "Saturday", 14 },
                { "Sunday", 8 }
            }, 20.00f),

            // MORE EDGE CASES FOR COMPREHENSIVE TESTING

            (new Dictionary<string, int>
            {
                { "Monday", 8 },
                { "Tuesday", 8 },
                { "Wednesday", 8 },
                { "Thursday", 8 },
                { "Friday", 7 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 26.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 9 },
                { "Wednesday", 9 },
                { "Thursday", 9 },
                { "Friday", 9 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 29.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 34.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 9 },
                { "Wednesday", 9 },
                { "Thursday", 9 },
                { "Friday", 9 },
                { "Saturday", 5 },
                { "Sunday", 5 }
            }, 31.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 8 },
                { "Sunday", 7 }
            }, 28.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 12 },
                { "Wednesday", 12 },
                { "Thursday", 12 },
                { "Friday", 12 },
                { "Saturday", 8 },
                { "Sunday", 4 }
            }, 85.00f),

            // UNUSUAL DISTRIBUTIONS

            (new Dictionary<string, int>
            {
                { "Monday", 24 },
                { "Tuesday", 0 },
                { "Wednesday", 0 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 0 },
                { "Sunday", 0 }
            }, 45.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 0 },
                { "Wednesday", 25 },
                { "Thursday", 0 },
                { "Friday", 0 },
                { "Saturday", 25 },
                { "Sunday", 0 }
            }, 32.25f),

            (new Dictionary<string, int>
            {
                { "Monday", 4 },
                { "Tuesday", 5 },
                { "Wednesday", 6 },
                { "Thursday", 7 },
                { "Friday", 8 },
                { "Saturday", 9 },
                { "Sunday", 10 }
            }, 24.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 10 },
                { "Wednesday", 8 },
                { "Thursday", 6 },
                { "Friday", 4 },
                { "Saturday", 2 },
                { "Sunday", 0 }
            }, 41.50f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 0 },
                { "Wednesday", 10 },
                { "Thursday", 0 },
                { "Friday", 10 },
                { "Saturday", 0 },
                { "Sunday", 8 }
            }, 36.25f),

            // FINAL STRESS TESTS

            (new Dictionary<string, int>
            {
                { "Monday", 15 },
                { "Tuesday", 15 },
                { "Wednesday", 15 },
                { "Thursday", 15 },
                { "Friday", 15 },
                { "Saturday", 12 },
                { "Sunday", 8 }
            }, 13.25f),

            (new Dictionary<string, int>
            {
                { "Monday", 4 },
                { "Tuesday", 0 },
                { "Wednesday", 3 },
                { "Thursday", 0 },
                { "Friday", 2 },
                { "Saturday", 0 },
                { "Sunday", 1 }
            }, 150.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 10 },
                { "Tuesday", 10 },
                { "Wednesday", 10 },
                { "Thursday", 10 },
                { "Friday", 10 },
                { "Saturday", 10 },
                { "Sunday", 10 }
            }, 26.75f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 12 },
                { "Wednesday", 12 },
                { "Thursday", 12 },
                { "Friday", 12 },
                { "Saturday", 10 },
                { "Sunday", 10 }
            }, 30.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 12 },
                { "Tuesday", 12 },
                { "Wednesday", 12 },
                { "Thursday", 12 },
                { "Friday", 12 },
                { "Saturday", 10 },
                { "Sunday", 10 }
            }, 25.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 9 },
                { "Tuesday", 9 },
                { "Wednesday", 9 },
                { "Thursday", 9 },
                { "Friday", 9 },
                { "Saturday", 7 },
                { "Sunday", 6 }
            }, 33.00f),

            (new Dictionary<string, int>
            {
                { "Monday", 0 },
                { "Tuesday", 14 },
                { "Wednesday", 0 },
                { "Thursday", 16 },
                { "Friday", 0 },
                { "Saturday", 12 },
                { "Sunday", 0 }
            }, 95.00f)
        ];
    }

    private static float GetTotalHours(Schedule schedule) // Changed return type to float
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
}
