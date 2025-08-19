using Bogus;
using Microsoft.EntityFrameworkCore;
using PolyMod.RecommendationModule.Data;
using PolyMod.RecommendationModule.Models.Recommendations;
using PolyMod.RecommendationModule.Repositories.Interfaces;
using PolyMod.MetricsModule.Services;
using PolyMod.MetricsModule.Services.Interfaces;
using PolyMod.ScheduleModule.Models;
using PolyMod.ServiceModule.Models;
using PolyMod.UserModule.Models;
using DomainUser = PolyMod.UserModule.Models.User;

namespace PolyMod.RecommendationModule.Seed;

public class RecommendationSeederAndTrainer(
    IServiceProvider serviceProvider,
    ILogger<RecommendationSeederAndTrainer> logger,
    IRecommendationOutputRepository outputRepository)
{
    private readonly Random _random = new();

    /// <summary>
    /// Main seeding method - handles the complete seeding process
    /// </summary>
    public async Task SeedRecommendationsAsync(
        List<DomainUser> users,
        List<Service> services,
        bool recreateDatabase = true,
        bool includeRatings = true,
        bool generateRecommendationOutputs = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();
        var metricsService = GetMetricsService(scope);

        try
        {
            logger.LogInformation("🚀 Starting recommendation seeding process...");

            // Step 1: Database preparation
            if (recreateDatabase)
            {
                await PrepareDatabase(context);
            }

            // Step 2: Validate input data
            ValidateInputData(users, services);

            // Step 3: Seed base entities (users and services)
            await SeedBaseEntities(context, users, services);

            // Step 4: ⭐ NEW - Seed schedules for users
            await SeedSchedulesIntoRecommendationContext(context, users);

            // Step 5: Generate and seed user recommendations (historical data)
            var recommendations = await GenerateRecommendations(users, services, includeRatings);
            await SeedRecommendations(context, recommendations);

            // Step 6: Generate and seed recommendation outputs (ML generated recommendations)
            if (generateRecommendationOutputs)
            {
                await GenerateAndSeedRecommendationOutputs(users, services);
            }

            // Step 7: Log statistics
            await LogSeedingStatistics(context);

            logger.LogInformation("✅ Recommendation seeding completed successfully!");
            metricsService.IncrementCounter("seeding.recommendation_success");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during recommendation seeding");
            metricsService.IncrementCounter("seeding.recommendation_error");
            throw;
        }
    }

    private async Task SeedSchedulesIntoRecommendationContext(RecommendationDbContext context,
        List<DomainUser> users)
    {
        logger.LogInformation("📅 Seeding schedules into recommendation context...");
        var metricsService = GetMetricsService(serviceProvider.CreateScope());

        metricsService.IncrementCounter("seeding.schedule_seed_started");

        var schedules = new List<Schedule>();

        // Create specific test case schedules (boundary testing)
        schedules.AddRange(CreateBoundaryTestSchedules(users.Take(14).ToList()));

        // Create realistic schedules for remaining users
        var remainingUsers = users.Skip(14).ToList();
        schedules.AddRange(CreateRealisticSchedules(remainingUsers));

        // Calculate total hours for all schedules
        foreach (var schedule in schedules)
        {
            schedule.RecalculateTotalHours();
            EnsureProperTimestamps(schedule);
        }

        // Track schedule categories for metrics
        await TrackScheduleMetrics(schedules, metricsService);

        // Save to the database
        await context.Schedules.AddRangeAsync(schedules);
        var savedCount = await context.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.schedule_database_save_completed");
        metricsService.IncrementCounter("seeding.schedule_seed_completed");

        logger.LogInformation("✅ Seeded {ScheduleCount} schedules with varied overtime patterns (saved {SavedCount})",
            schedules.Count, savedCount);
    }

    private List<Schedule> CreateBoundaryTestSchedules(List<DomainUser> testUsers)
    {
        var schedules = new List<Schedule>();
        var scheduleTemplates = GetRandomizedSchedules();

        for (var i = 0; i < Math.Min(testUsers.Count, scheduleTemplates.Count); i++)
        {
            var user = testUsers[i];
            var template = scheduleTemplates[i];

            var schedule = new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                BasePay = (float?)template.BasePay,
                DaysWorked = template.DaysWorked
            };

            schedules.Add(schedule);
        }

        return schedules;
    }

    private static List<(Dictionary<string, int> DaysWorked, double BasePay)> GetRandomizedSchedules(int count = 10)
    {
        var faker = new Faker();
        var weekdays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        var schedules = new List<(Dictionary<string, int>, double)>();

        for (var i = 0; i < count; i++)
        {
            var schedule = new Dictionary<string, int>();
            var totalHours = 0;

            foreach (var day in weekdays)
            {
                // More hours on weekdays, less on weekends
                var hours = day switch
                {
                    "Saturday" or "Sunday" => faker.Random.Int(0, 8),
                    _ => faker.Random.Int(6, 10)
                };

                schedule[day] = hours;
                totalHours += hours;
            }

            // Base pay loosely tied to workload range and randomness
            var basePay = faker.Random.Double(15, 50);

            switch (totalHours)
            {
                // Bump pay if heavy schedule
                case > 60:
                    basePay += faker.Random.Double(5, 20);
                    break;
                case < 20:
                    basePay -= faker.Random.Double(0, 5);
                    break;
            }

            // Ensure it's not below minimum wage
            basePay = Math.Round(Math.Max(basePay, 12.00), 2);

            schedules.Add((schedule, basePay));
        }

        return schedules;
    }


    private List<Schedule> CreateRealisticSchedules(List<DomainUser> users)
    {
        return users.Select(GenerateRealisticSchedule).ToList();
    }

    private Schedule GenerateRealisticSchedule(DomainUser user)
    {
        var totalHours = GenerateRealisticTotalHours();
        var daysWorked = DistributeHoursAcrossDays(totalHours);
        var basePay = GenerateRealisticBasePay();

        return new Schedule { Id = Guid.NewGuid(), UserId = user.Id, BasePay = basePay, DaysWorked = daysWorked };
    }

    private int GenerateRealisticTotalHours()
    {
        var random = _random.NextDouble();

        return random switch
        {
            < 0.05 => _random.Next(0, 20), // 5% part-time/minimal
            < 0.60 => _random.Next(35, 45), // 55% standard full-time (35-44 hours)
            < 0.85 => _random.Next(45, 55), // 25% moderate overtime (45-54 hours)
            < 0.95 => _random.Next(55, 70), // 10% high overtime (55-69 hours)
            _ => _random.Next(70, 90) // 5% extreme hours (70-89 hours)
        };
    }

    private Dictionary<string, int> DistributeHoursAcrossDays(int totalHours)
    {
        var days = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        var daysWorked = new Dictionary<string, int>();
        var remainingHours = totalHours;

        // Initialize all days to 0
        foreach (var day in days)
        {
            daysWorked[day] = 0;
        }

        // If zero hours, return empty schedule
        if (totalHours == 0) return daysWorked;

        // Distribute hours with realistic patterns
        var workingDays = DetermineWorkingDaysCount(totalHours);
        var selectedDays = SelectWorkingDays(days, workingDays);

        // Distribute hours among selected days
        foreach (var day in selectedDays.OrderBy(_ => _random.Next()))
        {
            if (remainingHours <= 0) break;

            var maxHoursForDay = Math.Min(remainingHours, DetermineMaxHoursPerDay());
            var hoursToday = _random.Next(1, maxHoursForDay + 1);

            daysWorked[day] = hoursToday;
            remainingHours -= hoursToday;
        }

        // Distribute any remaining hours
        while (remainingHours > 0)
        {
            var dayToAddTo = selectedDays[_random.Next(selectedDays.Length)];
            daysWorked[dayToAddTo]++;
            remainingHours--;
        }

        return daysWorked;
    }

    private int DetermineWorkingDaysCount(int totalHours)
    {
        return totalHours switch
        {
            0 => 0,
            <= 20 => _random.Next(1, 4), // 1-3 days for part-time
            <= 40 => _random.Next(4, 6), // 4-5 days for a standard
            <= 60 => _random.Next(5, 7), // 5-6 days for overtime
            _ => _random.Next(6, 8) // 6-7 days for extreme hours
        };
    }

    private string[] SelectWorkingDays(string[] allDays, int workingDaysCount)
    {
        if (workingDaysCount >= allDays.Length) return allDays;

        // Prefer weekdays for lower hour counts
        if (workingDaysCount > 5)
        {
            return allDays.OrderBy(_ => _random.Next()).Take(workingDaysCount).ToArray();
        }

        {
            var weekdays = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
            return weekdays.OrderBy(_ => _random.Next()).Take(workingDaysCount).ToArray();
        }

        // For higher hour counts, include weekends
    }

    private int DetermineMaxHoursPerDay()
    {
        var random = _random.NextDouble();

        return random switch
        {
            < 0.70 => 12, // 70% normal days (up to 12 hours)
            < 0.90 => 16, // 20% long days (up to 16 hours)
            _ => 20 // 10% extreme days (up to 20 hours)
        };
    }

    private float GenerateRealisticBasePay() // Changed return type to float
    {
        var random = _random.NextSingle();

        return random switch
        {
            < 0.20f => _random.Next(12, 18),
            < 0.60f => _random.Next(18, 30),
            < 0.85f => _random.Next(30, 45),
            < 0.95f => _random.Next(45, 65),
            _ => _random.Next(65, 100)
        };
    }


    private async Task TrackScheduleMetrics(List<Schedule> schedules, IMetricsService metricsService)
    {
        var regularTimeSchedules = schedules.Count(s => GetTotalHours(s) <= 40);
        var overtimeSchedules = schedules.Count(s => GetTotalHours(s) > 40 && GetTotalHours(s) <= 60);
        var doubleOvertimeSchedules = schedules.Count(s => GetTotalHours(s) > 60);
        var zeroHourSchedules = schedules.Count(s => GetTotalHours(s) == 0);
        var extremeHourSchedules = schedules.Count(s => GetTotalHours(s) > 80);
        var highPaySchedules = schedules.Count(s => s.BasePay > 50.00);
        var lowPaySchedules = schedules.Count(s => s.BasePay < 20.00);
        var boundarySchedules = schedules.Count(IsBoundarySchedule);
        var weekendWorkSchedules = schedules.Count(HasWeekendWork);
        var singleDaySchedules = schedules.Count(IsSingleDaySchedule);

        // Log metrics
        metricsService.IncrementCounter($"seeding.schedules_created_total.{schedules.Count}");
        metricsService.IncrementCounter($"seeding.schedules_created_regular_time.{regularTimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_overtime.{overtimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_double_overtime.{doubleOvertimeSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_zero_hours.{zeroHourSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_extreme_hours.{extremeHourSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_high_pay.{highPaySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_low_pay.{lowPaySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_boundary_test.{boundarySchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_weekend_work.{weekendWorkSchedules}");
        metricsService.IncrementCounter($"seeding.schedules_created_single_day.{singleDaySchedules}");

        await Task.CompletedTask; // For async consistency
    }

    private static int GetTotalHours(Schedule schedule) => schedule.DaysWorked.Values.Sum();

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


    /// <summary>
    /// Generate and seed recommendation outputs (ML-generated recommendations)
    /// </summary>
    private async Task GenerateAndSeedRecommendationOutputs(List<DomainUser> users, List<Service> services)
    {
        logger.LogInformation("🤖 Generating ML recommendation outputs...");

        const int userBatchSize = 100; // Process users in smaller batches
        const int recommendationBatchSize = 1000; // Save recommendations in batches
        var totalUsers = users.Count;
        var totalBatches = (int)Math.Ceiling((double)totalUsers / userBatchSize);
        var totalRecommendationsGenerated = 0;

        // Pre-create service array for efficient random selection
        var serviceArray = services.ToArray();
        var serviceCount = serviceArray.Length;

        for (var batchIndex = 0; batchIndex < totalBatches; batchIndex++)
        {
            var userBatch = users.Skip(batchIndex * userBatchSize).Take(userBatchSize).ToList();
            var batchId = Guid.NewGuid();
            var recommendationOutputs = new List<RecommendationOutput>();

            logger.LogInformation("Processing user batch {BatchIndex}/{TotalBatches} ({UserCount} users)",
                batchIndex + 1, totalBatches, userBatch.Count);

            // Generate recommendations for this batch of users
            foreach (var user in userBatch)
            {
                var recommendationCount = _random.Next(5, 16);
                var selectedServices = SelectRandomServices(serviceArray, serviceCount, recommendationCount);

                recommendationOutputs.AddRange(selectedServices.Select((t, i) =>
                    CreateRecommendationOutput(user.Id, t.Id, batchId, i + 1)));

                // Save in smaller batches to prevent memory buildup
                if (recommendationOutputs.Count < recommendationBatchSize)
                {
                    continue;
                }

                await outputRepository.SaveRecommendationBatchAsync(recommendationOutputs);
                totalRecommendationsGenerated += recommendationOutputs.Count;
                recommendationOutputs.Clear(); // Free memory immediately
            }

            // Save any remaining recommendations from this user batch
            if (recommendationOutputs.Count > 0)
            {
                await outputRepository.SaveRecommendationBatchAsync(recommendationOutputs);
                totalRecommendationsGenerated += recommendationOutputs.Count;
            }

            logger.LogInformation("✅ Completed user batch {BatchIndex}/{TotalBatches}",
                batchIndex + 1, totalBatches);
        }

        logger.LogInformation("✅ Generated and saved {OutputCount} recommendation outputs for {UserCount} users",
            totalRecommendationsGenerated, totalUsers);
    }

    private Service[] SelectRandomServices(Service[] services, int serviceCount, int count)
    {
        // Ensure we don't try to select more services than available
        var selectCount = Math.Min(count, serviceCount);

        // Create array of indices for shuffling
        var indices = new int[serviceCount];
        for (var i = 0; i < serviceCount; i++)
        {
            indices[i] = i;
        }

        // Partial Fisher-Yates shuffle - only shuffle what we need
        for (var i = 0; i < selectCount; i++)
        {
            var j = _random.Next(i, serviceCount);
            (indices[i], indices[j]) = (indices[j], indices[i]);
        }

        // Return selected services
        var result = new Service[selectCount];
        for (var i = 0; i < selectCount; i++)
        {
            result[i] = services[indices[i]];
        }

        return result;
    }

    private RecommendationOutput CreateRecommendationOutput(Guid userId, Guid serviceId, Guid batchId, int rank)
    {
        var faker = new Faker();
        var createdAt = faker.Date.Past(10);


        var updatedAt = faker.Date.Between(createdAt, DateTime.UtcNow);

        var generatedAt = faker.Date.Between(updatedAt, DateTime.UtcNow);

        var hasBeenViewed = faker.Random.Bool();
        var hasBeenClicked = faker.Random.Bool();
        var hasBeenDeleted = faker.Random.Bool();
        var viewedAt = hasBeenViewed ? faker.Date.Between(createdAt, updatedAt) : (DateTime?)null;
        var clickedAt = hasBeenClicked ? faker.Date.Between(createdAt, updatedAt) : (DateTime?)null;
        var deletedAt = hasBeenDeleted ? faker.Date.Between(createdAt, updatedAt) : (DateTime?)null;
        return new RecommendationOutput
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            BatchId = batchId,
            Rank = rank,
            HasBeenViewed = hasBeenViewed,
            HasBeenClicked = hasBeenClicked,
            ViewedAt = viewedAt,
            ClickedAt = clickedAt,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
            DeletedAt = deletedAt,
            GeneratedAt = generatedAt,
            Score = GenerateRealisticScore(rank),
            Strategy = faker.PickRandom(GenerateRecommendationStrategy()),
            Context = faker.PickRandom(GenerateRecommendationContext())
        };
    }


    /// <summary>
    /// Generate realistic ML confidence scores (higher scores for better ranks)
    /// </summary>
    private float GenerateRealisticScore(int rank)
    {
        // Higher ranks (1, 2, 3) should have higher scores
        var baseScore = rank switch
        {
            1 => _random.NextSingle() * 0.2f + 0.8f, // 0.8-1.0
            2 => _random.NextSingle() * 0.15f + 0.7f, // 0.7-0.85
            3 => _random.NextSingle() * 0.15f + 0.6f, // 0.6-0.75
            <= 5 => _random.NextSingle() * 0.2f + 0.4f, // 0.4-0.6
            <= 10 => _random.NextSingle() * 0.2f + 0.2f, // 0.2-0.4
            _ => _random.NextSingle() * 0.2f + 0.1f // 0.1-0.3
        };

        return (float)Math.Round(baseScore, 3);
    }

    /// <summary>
    /// Generate recommendation context
    /// </summary>
    private string GenerateRecommendationContext()
    {
        var contexts = new[]
        {
            "morning_routine", "evening_relaxation", "weekend_activities", "workday_break", "lunch_time",
            "after_work", "weekend_morning", "late_night", "holiday_special", "seasonal_recommendation",
            "similar_users", "trending_in_area", "based_on_history", "cold_start_user", "popular_this_week",
            "seasonal_boost", "cross_category", "price_sensitive", "quality_focused"
        };

        return contexts[_random.Next(contexts.Length)];
    }

    /// <summary>
    /// Generate a recommendation strategy
    /// </summary>
    private string GenerateRecommendationStrategy()
    {
        var strategies = new[]
        {
            "MatrixFactorization", "CollaborativeFiltering", "ContentBased", "Hybrid", "PopularityBased",
            "TrendingNow", "PersonalizedRanking"
        };

        return strategies[_random.Next(strategies.Length)];
    }

    /// <summary>
    /// Prepare database - clean slate approach
    /// </summary>
    private async Task PrepareDatabase(RecommendationDbContext context)
    {
        logger.LogInformation("🗄️ Preparing database...");

        try
        {
            // Ensure the database is deleted and recreated
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            logger.LogInformation("✅ Database prepared successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error preparing database");
            throw;
        }
    }

    /// <summary>
    /// Validate input data before seeding
    /// </summary>
    private void ValidateInputData(List<DomainUser> users, List<Service> services)
    {
        if (users == null || users.Count == 0)
        {
            throw new ArgumentException("Users list cannot be null or empty", nameof(users));
        }

        if (services == null || services.Count == 0)
        {
            throw new ArgumentException("Services list cannot be null or empty", nameof(services));
        }

        // Validate users have proper IDs
        var invalidUsers = users.Where(u => u.Id == Guid.Empty).ToList();
        if (invalidUsers.Count != 0)
        {
            throw new ArgumentException($"Found {invalidUsers.Count} users with empty GUIDs");
        }

        // Validate services have proper IDs
        var invalidServices = services.Where(s => s.Id == Guid.Empty).ToList();
        if (invalidServices.Count != 0)
        {
            throw new ArgumentException($"Found {invalidServices.Count} services with empty GUIDs");
        }

        logger.LogInformation("✅ Input data validation passed - {UserCount} users, {ServiceCount} services",
            users.Count, services.Count);
    }

    /// <summary>
    /// Seed base entities (users and services) with proper relationship handling
    /// </summary>
    private async Task SeedBaseEntities(RecommendationDbContext context, List<DomainUser> users, List<Service> services)
    {
        logger.LogInformation("👥 Seeding base entities...");


        // Prepare services with proper timestamps
        foreach (var servicesId in services)
        {
            EnsureProperTimestamps(servicesId);
        }

        // Add entities to context
        await context.Users.AddRangeAsync(users);
        await context.Services.AddRangeAsync(services);
        await context.SaveChangesAsync();
        // Save in one transaction
        var savedCount = await context.SaveChangesAsync();

        logger.LogInformation("💾 Saved {SavedCount} base entities ({UserCount} users, {ServiceCount} services)",
            savedCount, users.Count, services.Count);
    }

    /// <summary>
    /// Generate realistic recommendations with proper distribution
    /// </summary>
    private Task<List<UserRecommendation>> GenerateRecommendations(List<DomainUser> users,
        List<Service> services,
        bool includeRatings)
    {
        logger.LogInformation("📝 Generating user recommendations...");

        var serviceIds = services.Select(s => s.Id).ToList();

        var recommendations = (from user in users
            let recommendationCount = _random.Next(3, 13)
            let selectedServiceIds = serviceIds.OrderBy(_ => _random.Next())
                .Take(recommendationCount)
                .ToList()
            from serviceId in selectedServiceIds
            select CreateRecommendation(user.Id, serviceId, includeRatings)).ToList();

        logger.LogInformation("✅ Generated {RecommendationCount} user recommendations", recommendations.Count);
        return Task.FromResult(recommendations);
    }

    /// <summary>
    /// Create a single recommendation with realistic data
    /// </summary>
    private UserRecommendation CreateRecommendation(Guid userId, Guid serviceId, bool includeRatings)
    {
        var daysAgo = _random.Next(0, 90); // Spread over 3 months
        var recommendedAt = DateTime.UtcNow.AddDays(-daysAgo);

        var clickCount = GenerateRealisticClickCount();
        var rating = includeRatings ? GenerateRealisticRating(clickCount) : 0f;

        return new UserRecommendation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ServiceId = serviceId,
            RecommendedAt = recommendedAt,
            ClickCount = clickCount,
            Rating = rating,
            CreatedAt = recommendedAt,
            UpdatedAt = recommendedAt
        };
    }

    /// <summary>
    /// Generate realistic click counts following power law distribution
    /// </summary>
    private int GenerateRealisticClickCount()
    {
        var random = _random.NextDouble();

        return random switch
        {
            < 0.5 => 0, // 50% no clicks
            < 0.75 => 1, // 25% one click
            < 0.9 => _random.Next(2, 5), // 15% moderate engagement (2-4 clicks)
            < 0.98 => _random.Next(5, 10), // 8% high engagement (5-9 clicks)
            _ => _random.Next(10, 20) // 2% very high engagement (10-19 clicks)
        };
    }

    /// <summary>
    /// Generate realistic ratings based on engagement
    /// </summary>
    private float GenerateRealisticRating(int clickCount)
    {
        // Base rating influenced by engagement level
        var baseRating = clickCount switch
        {
            0 => GenerateRatingInRange(1.0f, 3.0f), // Low rating for no engagement
            1 => GenerateRatingInRange(2.0f, 4.0f), // Mixed for a single click
            >= 2 and <= 4 => GenerateRatingInRange(3.0f, 5.0f), // Good for moderate
            >= 5 and <= 9 => GenerateRatingInRange(4.0f, 5.0f), // High for good engagement
            _ => GenerateRatingInRange(4.5f, 5.0f) // Excellent for very high engagement
        };

        // Add some randomness but keep realistic
        var noise = (_random.NextSingle() - 0.5f) * 0.3f; // ±0.15 rating noise
        baseRating = Math.Max(1.0f, Math.Min(5.0f, baseRating + noise));
        baseRating = (float)(Math.Round(baseRating * 2, MidpointRounding.AwayFromZero) / 2);
        if (_random.NextDouble() < 0.1)
        {
            baseRating = Math.Max(1.0f, baseRating - 2.0f);
        }

        // Round to the nearest 0.5 for realistic ratings (1.0, 1.5, 2.0, etc.)
        return Math.Max(1.0f, Math.Min(5.0f, baseRating));
    }

    private float GenerateRatingInRange(float min, float max)
    {
        return min + _random.NextSingle() * (max - min);
    }

    /// <summary>
    /// Seed recommendations in batches for better performance
    /// </summary>
    private async Task SeedRecommendations(RecommendationDbContext context, List<UserRecommendation> recommendations)
    {
        logger.LogInformation("💾 Seeding {RecommendationCount} user recommendations...", recommendations.Count);

        const int batchSize = 1000;
        var totalBatches = (int)Math.Ceiling((double)recommendations.Count / batchSize);
        var savedCount = 0;

        for (var i = 0; i < totalBatches; i++)
        {
            var batch = recommendations.Skip(i * batchSize).Take(batchSize).ToList();

            await context.UserRecommendations.AddRangeAsync(batch);
            var batchSaved = await context.SaveChangesAsync();
            savedCount += batchSaved;

            logger.LogInformation("📦 Saved batch {BatchNumber}/{TotalBatches} ({BatchSaved} recommendations)",
                i + 1, totalBatches, batchSaved);
        }

        logger.LogInformation("✅ Successfully seeded {SavedCount} user recommendations", savedCount);
    }

    /// <summary>
    /// Log comprehensive seeding statistics
    /// </summary>
    private async Task LogSeedingStatistics(RecommendationDbContext context)
    {
        IMetricsServiceFactory serviceFactory = new MetricsServiceFactory();
        var service = serviceFactory.CreateMetricsService("LogSeedingStatistics");
        logger.LogInformation("📊 Computing seeding statistics...");

        try
        {
            var totalUsers = await context.Users.CountAsync();
            var totalSchedules = await context.Schedules.CountAsync();
            var totalRecommendations = await context.UserRecommendations.CountAsync();
            var totalOutputs = await context.RecommendationOutputs.CountAsync();
            var totalWithRatings = await context.UserRecommendations.CountAsync(r => r.Rating > 0);
            var totalClicks = await context.UserRecommendations.SumAsync(r => r.ClickCount);
            var totalViewed = await context.RecommendationOutputs.CountAsync(r => r.HasBeenViewed);
            var totalClicked = await context.RecommendationOutputs.CountAsync(r => r.HasBeenClicked);

            var avgRating = totalWithRatings > 0
                ? await context.UserRecommendations.Where(r => r.Rating > 0).AverageAsync(r => r.Rating)
                : 0;

            var avgRecommendationsPerUser = totalUsers > 0 ? (double)totalRecommendations / totalUsers : 0;
            var avgOutputsPerUser = totalUsers > 0 ? (double)totalOutputs / totalUsers : 0;

            // ⭐ NEW - Schedule statistics
            var scheduleStats = totalSchedules > 0 ? await GetScheduleStatistics(context) : null;

            logger.LogInformation("📈 Seeding Statistics:");
            service.IncrementCounter("========================>📈 Seeding Statistics<========================");
            logger.LogInformation("   • Users: {TotalUsers}", totalUsers);
            logger.LogInformation("   • Schedules: {TotalSchedules}", totalSchedules); // ⭐ NEW
            logger.LogInformation("   • User Recommendations: {TotalRecommendations}", totalRecommendations);
            logger.LogInformation("   • ML Recommendation Outputs: {TotalOutputs}", totalOutputs);
            logger.LogInformation("   • Recommendations with Ratings: {TotalWithRatings}", totalWithRatings);
            logger.LogInformation("   • Rating Coverage: {RatingCoverage:P1}",
                (double)totalWithRatings / totalRecommendations);
            logger.LogInformation("   • Average Rating: {AvgRating:F2}", avgRating);
            logger.LogInformation("   • Total Clicks: {TotalClicks}", totalClicks);
            logger.LogInformation("   • Outputs Viewed: {TotalViewed}", totalViewed);
            logger.LogInformation("   • Outputs Clicked: {TotalClicked}", totalClicked);
            logger.LogInformation("   • Avg User Recommendations per User: {AvgRecommendationsPerUser:F1}",
                avgRecommendationsPerUser);
            logger.LogInformation("   • Avg ML Outputs per User: {AvgOutputsPerUser:F1}",
                avgOutputsPerUser);
            await Task.Delay(500);
            // ⭐ NEW - Log schedule statistics
            if (scheduleStats != null)
            {
                logger.LogInformation("📅 Schedule Statistics:");
                logger.LogInformation("   • Average Hours per Schedule: {AvgHours:F1}", scheduleStats.AvgHours);
                logger.LogInformation("   • Average Base Pay: ${AvgPay:F2}", scheduleStats.AvgBasePay);
                logger.LogInformation("   • Regular Time Schedules: {RegularTime}", scheduleStats.RegularTimeCount);
                logger.LogInformation("   • Overtime Schedules: {Overtime}", scheduleStats.OvertimeCount);
                logger.LogInformation("   • Double Overtime Schedules: {DoubleOvertime}",
                    scheduleStats.DoubleOvertimeCount);
            }

            // Log to metrics service
            service.IncrementCounter($"stats.total_users_{totalUsers}");
            service.IncrementCounter($"stats.total_schedules_{totalSchedules}");
            service.IncrementCounter($"stats.total_recommendations_{totalRecommendations}");
            service.IncrementCounter($"stats.total_outputs_{totalOutputs}");
            service.IncrementCounter($"avg.AverageRecommendationsPerUser_{avgRecommendationsPerUser:F1}");
            service.IncrementCounter($"avg.AverageOutputsPerUser_{avgOutputsPerUser:F1}");
            service.IncrementCounter($"avg.AverageRating_{avgRating:F2}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ Error computing statistics");
        }
    }

    private async Task<ScheduleStatistics> GetScheduleStatistics(RecommendationDbContext context)
    {
        var schedules = await context.Schedules.ToListAsync();

        return new ScheduleStatistics
        {
            AvgHours = schedules.Average(s => s.DaysWorked.Values.Sum()),
            AvgBasePay = schedules.Average(s => s.BasePay ?? 0),
            RegularTimeCount = schedules.Count(s => GetTotalHours(s) <= 40),
            OvertimeCount = schedules.Count(s => GetTotalHours(s) > 40 && GetTotalHours(s) <= 60),
            DoubleOvertimeCount = schedules.Count(s => GetTotalHours(s) > 60)
        };
    }


    /// <summary>
    /// Ensure entities have proper timestamps
    /// </summary>
    private void EnsureProperTimestamps(object entity)
    {
        var now = DateTime.UtcNow;

        switch (entity)
        {
            case DomainUser user:
                if (user.CreatedAt == default) user.CreatedAt = now.AddDays(-_random.Next(30, 365));
                user.UpdatedAt ??= now.AddDays(-_random.Next(0, 30));
                break;

            case Service service:
                if (service.CreatedAt == default) service.CreatedAt = now.AddDays(-_random.Next(60, 365));
                service.UpdatedAt ??= now.AddDays(-_random.Next(0, 60));
                break;

            case Schedule schedule:
                if (schedule.CreatedAt == default) schedule.CreatedAt = now.AddDays(-_random.Next(30, 365));
                schedule.UpdatedAt ??= now.AddDays(-_random.Next(0, 30));
                break;
        }
    }

    /// <summary>
    /// Get metrics service from scope
    /// </summary>
    private IMetricsService GetMetricsService(IServiceScope scope)
    {
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        return factory.CreateMetricsService("RecommendationSeeder");
    }
}
