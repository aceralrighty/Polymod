using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.ScheduleModule.Models;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Seed;

public class RecommendationSeederAndTrainer(
    IServiceProvider serviceProvider,
    ILogger<RecommendationSeederAndTrainer> logger)
{
    private readonly Random _random = new();

    /// <summary>
    /// Main seeding method - handles the complete seeding process
    /// </summary>
    public async Task SeedRecommendationsAsync(
        List<User> users,
        List<Service> services,
        bool recreateDatabase = true,
        bool includeRatings = true)
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

            // Step 4: Generate and seed recommendations
            var recommendations = await GenerateRecommendations(users, services, includeRatings);
            await SeedRecommendations(context, recommendations);

            // Step 5: Log statistics
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
            await context.Database.EnsureCreatedAsync();

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
    private void ValidateInputData(List<User> users, List<Service> services)
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
    private async Task SeedBaseEntities(RecommendationDbContext context, List<User> users, List<Service> services)
    {
        logger.LogInformation("👥 Seeding base entities...");

        // Prepare users with proper timestamps
        foreach (var user in users)
        {
            EnsureProperTimestamps(user);

            // Handle schedule relationship
            if (user.Schedule == null)
            {
                continue;
            }

            EnsureProperTimestamps(user.Schedule);
            user.Schedule.UserId = user.Id;
        }

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
    private Task<List<UserRecommendation>> GenerateRecommendations(List<User> users,
        List<Service> services,
        bool includeRatings)
    {
        logger.LogInformation("📝 Generating recommendations...");

        var serviceIds = services.Select(s => s.Id).ToList();

        var recommendations = (from user in users
            let recommendationCount = _random.Next(3, 13)
            let selectedServiceIds = serviceIds.OrderBy(_ => _random.Next())
                .Take(recommendationCount)
                .ToList()
            from serviceId in selectedServiceIds
            select CreateRecommendation(user.Id, serviceId, includeRatings)).ToList();

        logger.LogInformation("✅ Generated {RecommendationCount} recommendations", recommendations.Count);
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
            0 => GenerateRatingInRange(1.0f, 2.5f), // Low rating for no engagement
            1 => GenerateRatingInRange(2.0f, 4.0f), // Mixed for single click
            >= 2 and <= 4 => GenerateRatingInRange(3.0f, 5.0f), // Good for moderate
            >= 5 and <= 9 => GenerateRatingInRange(3.5f, 5.0f), // High for good engagement
            _ => GenerateRatingInRange(4.0f, 5.0f) // Excellent for very high engagement
        };

        // Add some randomness but keep realistic
        var noise = (_random.NextSingle() - 0.5f) * 0.3f; // ±0.15 rating noise
        baseRating = Math.Max(1.0f, Math.Min(5.0f, baseRating + noise));

        // Round to nearest 0.5 for realistic ratings (1.0, 1.5, 2.0, etc.)
        return (float)(Math.Round(baseRating * 2) / 2.0);
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
        logger.LogInformation("💾 Seeding {RecommendationCount} recommendations...", recommendations.Count);

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

        logger.LogInformation("✅ Successfully seeded {SavedCount} recommendations", savedCount);
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
            var totalRecommendations = await context.UserRecommendations.CountAsync();
            var totalWithRatings = await context.UserRecommendations.CountAsync(r => r.Rating > 0);
            var totalClicks = await context.UserRecommendations.SumAsync(r => r.ClickCount);

            var avgRating = totalWithRatings > 0
                ? await context.UserRecommendations.Where(r => r.Rating > 0).AverageAsync(r => r.Rating)
                : 0;

            var avgRecommendationsPerUser = totalUsers > 0 ? (double)totalRecommendations / totalUsers : 0;

            logger.LogInformation("📈 Seeding Statistics:");
            service.IncrementCounter("========================>📈 Seeding Statistics<========================");
            logger.LogInformation("   • Users: {TotalUsers}", totalUsers);
            service.IncrementCounter($"stats.total_users_{totalUsers}");
            logger.LogInformation("   • Total Recommendations: {TotalRecommendations}", totalRecommendations);
            service.IncrementCounter($"stats.total_recommendations_{totalRecommendations}");
            logger.LogInformation("   • Recommendations with Ratings: {TotalWithRatings}", totalWithRatings);
            service.IncrementCounter($"stats.recommendations_with_ratings_{totalWithRatings}");
            logger.LogInformation("   • Rating Coverage: {RatingCoverage:P1}",
                (double)totalWithRatings / totalRecommendations);
            service.IncrementCounter($"stats.rating_coverage_{(double)totalWithRatings / totalRecommendations}");
            logger.LogInformation("   • Average Rating: {AvgRating:F2}", avgRating);
            service.IncrementCounter($"avg.AverageRating_{avgRating:F2}");
            logger.LogInformation("   • Total Clicks: {TotalClicks}", totalClicks);
            logger.LogInformation("   • Avg Recommendations per User: {AvgRecommendationsPerUser:F1}",
                avgRecommendationsPerUser);
            service.IncrementCounter($"avg.AverageRecommendationsPerUser_{avgRating:F1}");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "⚠️ Error computing statistics");
        }
    }

    /// <summary>
    /// Ensure entities have proper timestamps
    /// </summary>
    private void EnsureProperTimestamps(object entity)
    {
        var now = DateTime.UtcNow;

        switch (entity)
        {
            case User user:
                if (user.CreatedAt == default) user.CreatedAt = now.AddDays(-_random.Next(30, 365));
                if (user.UpdatedAt == default) user.UpdatedAt = now.AddDays(-_random.Next(0, 30));
                break;

            case Service service:
                if (service.CreatedAt == default) service.CreatedAt = now.AddDays(-_random.Next(60, 365));
                if (service.UpdatedAt == default) service.UpdatedAt = now.AddDays(-_random.Next(0, 60));
                break;

            case Schedule schedule:
                if (schedule.CreatedAt == default) schedule.CreatedAt = now.AddDays(-_random.Next(30, 365));
                if (schedule.UpdatedAt == default) schedule.UpdatedAt = now.AddDays(-_random.Next(0, 30));
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
