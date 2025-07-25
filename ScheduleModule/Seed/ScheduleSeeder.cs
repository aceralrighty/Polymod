using System.Diagnostics;
using Bogus;
using Microsoft.EntityFrameworkCore;
using TBD.API.DTOs.Users;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ScheduleModule.Data;
using TBD.ScheduleModule.Models;
using TBD.Shared.Events.Interfaces;

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
        int numberOfAdditionalRandomSchedules = 500)
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

        metricsService.IncrementCounter("seeding.schedule_database_recreated");

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

        if (users.Count == 0)
        {
            Console.WriteLine("❌ No users found. Skipping schedule seeding.");
            activity?.SetTag("skipped_no_users", true);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        var existingSchedules = await scheduleContext.Schedules.ToListAsync();
        var usedUserIds = new HashSet<Guid>(existingSchedules.Select(s => s.UserId));
        var availableUsers = users.Where(u => !usedUserIds.Contains(u.Id)).ToList();

        var schedulesToSeed = new List<Schedule>();

        var testScheduleData = GetTestScheduleData();
        Console.WriteLine($"Adding {testScheduleData.Count} fixed test schedules.");

        foreach (var data in testScheduleData)
        {
            if (availableUsers.Count != 0)
            {
                var user = availableUsers[Random.Next(availableUsers.Count)];
                availableUsers.Remove(user);

                var newSchedule = new Schedule
                {
                    Id = Guid.NewGuid(), UserId = user.Id, DaysWorked = data.DaysWorked, BasePay = data.BasePay
                };
                newSchedule.RecalculateTotalHours();
                schedulesToSeed.Add(newSchedule);
            }
            else
            {
                Console.WriteLine("⚠️ Not enough unique users for all fixed test schedules. Skipping some.");
                break;
            }
        }

        switch (numberOfAdditionalRandomSchedules)
        {
            case > 0 when availableUsers.Count != 0:
            {
                Console.WriteLine($"Generating {numberOfAdditionalRandomSchedules} additional random schedules.");

                var scheduleFaker = new Faker<Schedule>()
                    .RuleFor(s => s.Id, _ => Guid.NewGuid())
                    .RuleFor(s => s.UserId, f => f.PickRandom(users.Select(u => u.Id).ToArray()))
                    .RuleFor(s => s.DaysWorked, f =>
                    {
                        var daysWorked = new Dictionary<string, int>();
                        var totalHours = f.Random.Int(0, 80);
                        var remainingHours = totalHours;
                        var days = StringArray.OrderBy(_ => f.Random.Guid()).ToArray();

                        foreach (var day in days)
                        {
                            if (remainingHours <= 0)
                            {
                                daysWorked[day] = 0;
                            }
                            else
                            {
                                var hoursToday = f.Random.Int(0, Math.Min(remainingHours, 14));
                                daysWorked[day] = hoursToday;
                                remainingHours -= hoursToday;
                            }
                        }

                        // Ensure all days present
                        foreach (var day in StringArray)
                        {
                            daysWorked.TryAdd(day, 0);
                        }

                        if (remainingHours <= 0)
                        {
                            return daysWorked;
                        }

                        var dayToAddTo = StringArray[f.Random.Int(0, StringArray.Length - 1)];
                        daysWorked[dayToAddTo] += remainingHours;

                        return daysWorked;
                    })
                    .RuleFor(s => s.BasePay, f => (float)Math.Round(f.Random.Double(12.00, 100.00), 2))
                    .RuleFor(s => s.CreatedAt, f => f.Date.Between(DateTime.UtcNow.AddYears(-10), DateTime.UtcNow))
                    .RuleFor(s => s.UpdatedAt, (f, s) => f.Date.Between(s.CreatedAt, DateTime.UtcNow))
                    .RuleFor(s => s.DeletedAt, (f, s) =>
                    {
                        // 30% chance to be deleted
                        if (f.Random.Bool(0.3f))
                        {
                            // DeletedAt after CreatedAt and before now
                            return f.Date.Between(s.CreatedAt, DateTime.UtcNow);
                        }

                        return null;
                    });


                var toGenerate = Math.Min(numberOfAdditionalRandomSchedules, availableUsers.Count);
                var newRandomSchedules = scheduleFaker.Generate(toGenerate);

                foreach (var schedule in newRandomSchedules)
                {
                    var user = availableUsers[Random.Next(availableUsers.Count)];
                    availableUsers.Remove(user);
                    schedule.UserId = user.Id;
                    schedule.RecalculateTotalHours();
                    schedulesToSeed.Add(schedule);
                }

                break;
            }
            case > 0 when availableUsers.Count == 0:
                Console.WriteLine("⚠️ No more available users to create additional random schedules.");
                break;
        }

        if (schedulesToSeed.Count != 0)
        {
            Console.WriteLine($"Saving {schedulesToSeed.Count} schedules to the database.");
            await scheduleContext.Schedules.AddRangeAsync(schedulesToSeed);
            await scheduleContext.SaveChangesAsync();
            metricsService.IncrementCounter("seeding.schedule_database_save_completed");
            Console.WriteLine($"✅ Seeded {schedulesToSeed.Count} new schedules.");

            LogScheduleMetrics(schedulesToSeed, metricsService, activity);
        }
        else
        {
            Console.WriteLine("🤷 No new schedules to seed.");
        }

        metricsService.IncrementCounter("seeding.schedule_seed_completed");
        activity?.SetStatus(ActivityStatusCode.Ok);
    }

    private static void LogScheduleMetrics(List<Schedule> schedules, IMetricsService metricsService, Activity? activity)
    {
        var regularTimeSchedules = schedules.Count(s => GetTotalHours(s) <= 40);
        var overtimeSchedules = schedules.Count(s => GetTotalHours(s) > 40 && GetTotalHours(s) <= 60);
        var doubleOvertimeSchedules = schedules.Count(s => GetTotalHours(s) > 60);
        var zeroHourSchedules = schedules.Count(s => GetTotalHours(s) == 0);
        var extremeHourSchedules = schedules.Count(s => GetTotalHours(s) > 80);
        var highPaySchedules = schedules.Count(s => s.BasePay > 50.00f);
        var lowPaySchedules = schedules.Count(s => s.BasePay < 20.00f);
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
        var faker = new Faker();
        var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        var generated = new HashSet<string>(); // To ensure uniqueness
        var result = new List<(Dictionary<string, int> DaysWorked, float BasePay)>();

        while (result.Count < 50)
        {
            var schedule = new Dictionary<string, int>();
            foreach (var day in daysOfWeek)
            {
                // Random hours: 0–12 per day
                schedule[day] = faker.Random.Int(0, 12);
            }

            // Random pay: $15–$100
            var basePay = (float)Math.Round(faker.Random.Float(15f, 100f), 2);

            // Ensure uniqueness by serializing schedule + pay as a key
            var uniquenessKey = string.Join(",", schedule.Values) + "|" + basePay;
            if (generated.Add(uniquenessKey))
            {
                result.Add((schedule, basePay));
            }
        }

        return result;
    }


    private static float GetTotalHours(Schedule schedule) => schedule.DaysWorked.Values.Sum();

    private static bool IsBoundarySchedule(Schedule schedule)
    {
        var totalHours = GetTotalHours(schedule);
        return totalHours is 40 or 41 or 60 or 61;
    }

    private static bool HasWeekendWork(Schedule schedule) =>
        schedule.DaysWorked.GetValueOrDefault("Saturday", 0) > 0 ||
        schedule.DaysWorked.GetValueOrDefault("Sunday", 0) > 0;

    private static bool IsSingleDaySchedule(Schedule schedule) =>
        schedule.DaysWorked.Values.Count(hours => hours > 0) == 1;
}
