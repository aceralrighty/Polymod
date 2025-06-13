using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.RecommendationModule.Services;
using TBD.ScheduleModule.Models;
using TBD.ServiceModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Seed;

public class RecommendationSeederAndTrainer(
    IServiceProvider serviceProvider,
    ILogger<RecommendationSeederAndTrainer> logger)
{
    private readonly Random _random = new();

    public static class RecommendationSeederUsage
    {
        public static async Task ExampleUsageAsync(IServiceProvider serviceProvider)
        {
            var users = await GetSampleUsersAsync();
            var services = await GetSampleServicesAsync();
            using var scope = serviceProvider.CreateScope();
            var recContext = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();

            var logger = serviceProvider.GetRequiredService<ILogger<RecommendationSeederAndTrainer>>();
            var seeder = new RecommendationSeederAndTrainer(serviceProvider, logger);
            await recContext.Database.EnsureDeletedAsync();
            await recContext.Database.EnsureCreatedAsync();

            await seeder.SeedAndTrainAsync(users, services, includeRatings: true);
            // Option 2: Seed the database
            await seeder.SeedRecommendationsWithRatingsAsync(users, services, includeRatings: true);

            // Option 3: Train the model (if data already exists)
            // await seeder.TrainRecommendationModelAsync();

            // Option 4: Add more ratings to existing data
            // await seeder.AddAdditionalRatingsAsync(500);
        }

        private static Task<List<User>> GetSampleUsersAsync()
        {
            var hashedPassword = new Hasher();
            var users = new List<User>();

            // Create users with proper schedule relationships
            var user1 = new User
            {
                Id = Guid.NewGuid(),
                Username = "john_doe",
                Email = "john@example.com",
                Password = hashedPassword.HashPassword("Sinners"),
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var user2 = new User
            {
                Id = Guid.NewGuid(),
                Username = "jane_smith",
                Email = "jane@example.com",
                Password = hashedPassword.HashPassword("Froogaloop"),
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var user3 = new User
            {
                Id = Guid.NewGuid(),
                Username = "bob_wilson",
                Email = "bob@example.com",
                Password = hashedPassword.HashPassword("why lord why"),
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            };

            // Create schedules with proper foreign key relationships
            var schedule1 = new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = user1.Id,
                BasePay = 25.00,
                DaysWorkedJson = "{\"Monday\": 8, \"Tuesday\": 8, \"Wednesday\": 8, \"Thursday\": 8, \"Friday\": 8, \"Saturday\": 0, \"Sunday\": 0}",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                UpdatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var schedule2 = new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = user2.Id,
                BasePay = 30.00,
                DaysWorkedJson = "{\"Monday\": 7, \"Tuesday\": 7, \"Wednesday\": 7, \"Thursday\": 7, \"Friday\": 7, \"Saturday\": 0, \"Sunday\": 0}",
                CreatedAt = DateTime.UtcNow.AddDays(-25),
                UpdatedAt = DateTime.UtcNow.AddDays(-2)
            };

            var schedule3 = new Schedule
            {
                Id = Guid.NewGuid(),
                UserId = user3.Id,
                BasePay = 20.00,
                DaysWorkedJson = "{\"Monday\": 9, \"Tuesday\": 9, \"Wednesday\": 9, \"Thursday\": 9, \"Friday\": 9, \"Saturday\": 0, \"Sunday\": 0}",
                CreatedAt = DateTime.UtcNow.AddDays(-20),
                UpdatedAt = DateTime.UtcNow.AddDays(-3)
            };

            // Set navigation properties
            user1.Schedule = schedule1;
            user2.Schedule = schedule2;
            user3.Schedule = schedule3;

            // Calculate total hours for schedules
            schedule1.RecalculateTotalHours();
            schedule2.RecalculateTotalHours();
            schedule3.RecalculateTotalHours();

            users.AddRange([user1, user2, user3]);
            return Task.FromResult(users);
        }

        private static Task<List<Service>> GetSampleServicesAsync()
        {
            var services = new List<Service>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Cloud Storage Service",
                    Description = "Secure cloud storage",
                    Price = (decimal)9.99,
                    DurationInMinutes = 0, // Subscription service
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Video Streaming",
                    Description = "Entertainment streaming platform",
                    Price = (decimal)12.99,
                    DurationInMinutes = 0, // Subscription service
                    CreatedAt = DateTime.UtcNow.AddDays(-55),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Title = "Online Learning",
                    Description = "Educational courses online",
                    Price = (decimal)29.99,
                    DurationInMinutes = 0, // Subscription service
                    CreatedAt = DateTime.UtcNow.AddDays(-50),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                }
            };

            return Task.FromResult(services);
        }
    }

    /// <summary>
    /// Complete workflow: Seed database with realistic data and train an ML model
    /// </summary>
    public async Task SeedAndTrainAsync(List<User> users, List<Service> services, bool includeRatings = true)
    {
        try
        {
            logger.LogInformation("🚀 Starting complete recommendation system seeding and training...");

            // Step 1: Seed database with recommendations and ratings
            await SeedRecommendationsWithRatingsAsync(users, services, includeRatings);

            // Step 2: Train the ML model
            await TrainRecommendationModelAsync();

            // Step 3: Validate the system
            await ValidateRecommendationSystemAsync(users.Take(3).ToList(), services);

            logger.LogInformation("✅ Complete recommendation system setup finished successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during recommendation system setup");
            throw;
        }
    }

    /// <summary>
    /// Seed database with realistic user-service interactions and ratings
    /// </summary>
    private async Task SeedRecommendationsWithRatingsAsync(List<User> users, List<Service> services,
        bool includeRatings = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("RecommendationSeeder");

        try
        {
            logger.LogInformation("🔄 Starting recommendation database seeding...");

            // Ensure clean database - consistent cleanup approach
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync(); // Use EnsureCreatedAsync instead of MigrateAsync for consistency

            logger.LogInformation("🗄️ Database recreated successfully");

            // Add users with their schedules first
            foreach (var user in users)
            {
                // Ensure the user and schedule have proper timestamps
                if (user.CreatedAt == default) user.CreatedAt = DateTime.UtcNow.AddDays(-30);
                if (user.UpdatedAt == default) user.UpdatedAt = DateTime.UtcNow;

                if (user.Schedule != null)
                {
                    if (user.Schedule.CreatedAt == default) user.Schedule.CreatedAt = user.CreatedAt;
                    if (user.Schedule.UpdatedAt == default) user.Schedule.UpdatedAt = user.UpdatedAt;
                    // Ensure proper foreign key relationship
                    user.Schedule.UserId = user.Id;
                }
            }

            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
            logger.LogInformation("👥 Added {UsersCount} users with schedules to context", users.Count);

            // Add services
            foreach (var service in services)
            {
                if (service.CreatedAt == default) service.CreatedAt = DateTime.UtcNow.AddDays(-60);
                if (service.UpdatedAt == default) service.UpdatedAt = DateTime.UtcNow.AddDays(-5);
            }

            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();
            logger.LogInformation("🎯 Added {ServicesCount} services to context", services.Count);

            // Generate realistic recommendations and ratings
            var recommendations = GenerateRealisticRecommendations(users, services, includeRatings);

            logger.LogInformation($"📝 Generated {recommendations.Count} recommendations with ratings");

            await context.UserRecommendations.AddRangeAsync(recommendations);
            var savedCount = await context.SaveChangesAsync();

            logger.LogInformation($"💾 Successfully saved {savedCount} recommendations to database");

            // Log statistics
            await LogSeededDataStatisticsAsync(context);

            metricsService.IncrementCounter("seeding.complete_with_ratings");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during database seeding");
            throw;
        }
    }

    /// <summary>
    /// Generate realistic user-service interactions with ratings following realistic patterns
    /// </summary>
    private List<UserRecommendation> GenerateRealisticRecommendations(List<User> users, List<Service> services,
        bool includeRatings)
    {
        var recommendations = new List<UserRecommendation>();
        var serviceCategories = CreateServiceCategories(services);

        foreach (var user in users)
        {
            // Each user gets 5-15 recommendations with varied interaction patterns
            var numberOfRecommendations = _random.Next(5, 16);

            // Create user preferences (some users prefer certain categories)
            var userPreferredCategories = GetUserPreferences(serviceCategories.Keys.ToList());

            // Select services based on preferences and randomness
            var userServices = SelectServicesForUser(services, serviceCategories, userPreferredCategories,
                numberOfRecommendations);

            foreach (var service in userServices)
            {
                var daysAgo = _random.Next(0, 90); // Spread interactions over 3 months
                var recommendedAt = DateTime.UtcNow.AddDays(-daysAgo);

                var recommendation = new UserRecommendation
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ServiceId = service.Id,
                    RecommendedAt = recommendedAt,
                    ClickCount = GenerateRealisticClickCount(),
                    CreatedAt = recommendedAt,
                    UpdatedAt = recommendedAt
                };

                // Add realistic ratings if requested
                if (includeRatings)
                {
                    recommendation.Rating =
                        GenerateRealisticRating(recommendation.ClickCount, userPreferredCategories, service);
                }

                recommendations.Add(recommendation);
            }
        }

        return recommendations;
    }

    /// <summary>
    /// Train the ML recommendation model using seeded data
    /// </summary>
    private async Task TrainRecommendationModelAsync()
    {
        using var scope = serviceProvider.CreateScope();
        var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();

        try
        {
            logger.LogInformation("🤖 Starting ML model training...");

            await recommendationService.TrainRecommendationModelAsync();

            logger.LogInformation("✅ ML model training completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error during ML model training");
            throw;
        }
    }

    /// <summary>
    /// Validate the recommendation system by testing predictions
    /// </summary>
    private async Task ValidateRecommendationSystemAsync(List<User> testUsers, List<Service> services)
    {
        using var scope = serviceProvider.CreateScope();
        var recommendationService = scope.ServiceProvider.GetRequiredService<IRecommendationService>();

        logger.LogInformation("🔍 Validating recommendation system...");

        foreach (var user in testUsers)
        {
            try
            {
                // Test ML recommendations
                var mlRecommendations = await recommendationService.GetMlRecommendationsAsync(user.Id, 5);
                logger.LogInformation(
                    "👤 User {UserUsername}: Generated {Count} ML recommendations", user.Username,
                    mlRecommendations.Count());

                // Test rating predictions for a few services
                var testServices = services.Take(3);
                foreach (var service in testServices)
                {
                    var predictedRating = await recommendationService.PredictRatingAsync(user.Id, service.Id);
                    logger.LogInformation($"   📊 Predicted rating for '{service.Title}': {predictedRating:F2}");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "⚠️ Validation issue for user {UserUsername}", user.Username);
            }
        }
    }

    /// <summary>
    /// Generate realistic click counts following power law distribution
    /// </summary>
    private int GenerateRealisticClickCount()
    {
        var random = _random.NextDouble();

        return random switch
        {
            < 0.4 => 0, // 40% no clicks (just recommended)
            < 0.7 => 1, // 30% one click
            < 0.85 => _random.Next(2, 4), // 15% moderate engagement
            < 0.95 => _random.Next(4, 8), // 10% high engagement
            _ => _random.Next(8, 15) // 5% very high engagement
        };
    }

    /// <summary>
    /// Generate realistic ratings based on click behavior and user preferences
    /// </summary>
    private float GenerateRealisticRating(int clickCount, List<string> userPreferences, Service service)
    {
        // Base rating influenced by engagement
        var baseRating = clickCount switch
        {
            0 => GenerateRatingInRange(1.0f, 3.0f), // Low rating if no clicks
            1 => GenerateRatingInRange(2.0f, 4.0f), // Mixed rating for a single click
            >= 2 and <= 4 => GenerateRatingInRange(3.0f, 5.0f), // Good rating for moderate engagement
            >= 5 and <= 8 => GenerateRatingInRange(3.5f, 5.0f), // High rating for high engagement
            _ => GenerateRatingInRange(4.0f, 5.0f) // Excellent rating for very high engagement
        };

        // Adjust rating based on user preferences (simulate user liking certain service types more)
        var serviceCategory = GetServiceCategory(service);
        if (userPreferences.Contains(serviceCategory))
        {
            baseRating = Math.Min(5.0f, baseRating + _random.Next(0, 10) * 0.1f); // Boost for a preferred category
        }

        // Add some randomness but keep it realistic
        var noise = (_random.NextSingle() - 0.5f) * 0.4f; // ±0.2 rating noise
        baseRating = Math.Max(1.0f, Math.Min(5.0f, baseRating + noise));

        // Round to the nearest 0.5 for realistic ratings
        return (float)(Math.Round(baseRating * 2) / 2.0);
    }

    private float GenerateRatingInRange(float min, float max)
    {
        return min + _random.NextSingle() * (max - min);
    }

    /// <summary>
    /// Create service categories for better recommendation logic
    /// </summary>
    private Dictionary<string, List<Service>> CreateServiceCategories(List<Service> services)
    {
        var categories = new Dictionary<string, List<Service>>();
        var categoryNames = new[]
        {
            "Technology", "Healthcare", "Education", "Finance", "Entertainment", "Travel", "Food", "Fitness"
        };

        // Simple categorization based on service index (in a real app, you'd use actual categories)
        foreach (var service in services)
        {
            var categoryIndex = Math.Abs(service.Id.GetHashCode()) % categoryNames.Length;
            var category = categoryNames[categoryIndex];

            if (!categories.ContainsKey(category))
                categories[category] = new List<Service>();

            categories[category].Add(service);
        }

        return categories;
    }

    /// <summary>
    /// Generate user preferences (which categories they prefer)
    /// </summary>
    private List<string> GetUserPreferences(List<string> availableCategories)
    {
        var preferenceCount = _random.Next(2, 5); // Each user prefers 2-4 categories
        return availableCategories.OrderBy(_ => _random.Next()).Take(preferenceCount).ToList();
    }

    /// <summary>
    /// Select services for a user based on preferences and some randomness
    /// </summary>
    private List<Service> SelectServicesForUser(List<Service> allServices, Dictionary<string, List<Service>> categories,
        List<string> userPreferences, int count)
    {
        var selectedServices = new List<Service>();

        // 70% from preferred categories, 30% random
        var preferredCount = (int)(count * 0.7);
        var randomCount = count - preferredCount;

        // Select from preferred categories
        var preferredServices = userPreferences
            .SelectMany(pref => categories.TryGetValue(pref, out var category) ? category : [])
            .Distinct()
            .OrderBy(_ => _random.Next())
            .Take(preferredCount);

        selectedServices.AddRange(preferredServices);

        // Fill the remaining with random services
        var remainingServices = allServices
            .Except(selectedServices)
            .OrderBy(_ => _random.Next())
            .Take(randomCount);

        selectedServices.AddRange(remainingServices);

        return selectedServices.Take(count).ToList();
    }

    private string GetServiceCategory(Service service)
    {
        var categoryNames = new[]
        {
            "Technology", "Healthcare", "Education", "Finance", "Entertainment", "Travel", "Food", "Fitness"
        };
        var categoryIndex = Math.Abs(service.Id.GetHashCode()) % categoryNames.Length;
        return categoryNames[categoryIndex];
    }

    /// <summary>
    /// Log statistics about the seeded data
    /// </summary>
    private async Task LogSeededDataStatisticsAsync(RecommendationDbContext context)
    {
        var totalRecommendations = await context.UserRecommendations.CountAsync();
        var totalWithRatings = await context.UserRecommendations.CountAsync(r => r.Rating > 0);
        var avgRating = await context.UserRecommendations
            .Where(r => r.Rating > 0)
            .AverageAsync(r => r.Rating);
        var totalClicks = await context.UserRecommendations.SumAsync(r => r.ClickCount);

        logger.LogInformation("📊 Seeded Data Statistics:");
        logger.LogInformation("   • Total Recommendations: {TotalRecommendations}", totalRecommendations);
        logger.LogInformation("   • Recommendations with Ratings: {TotalWithRatings}", totalWithRatings);
        logger.LogInformation("   • Average Rating: {AvgRating:F2}", avgRating);
        logger.LogInformation("   • Total Clicks: {TotalClicks}", totalClicks);
    }

    /// <summary>
    /// Add additional ratings to existing recommendations (useful for testing)
    /// </summary>
    public async Task AddAdditionalRatingsAsync(int numberOfRatings = 100)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();

        var unratedRecommendations = await context.UserRecommendations
            .Where(r => r.Rating == 0)
            .Take(numberOfRatings)
            .ToListAsync();

        foreach (var rec in unratedRecommendations)
        {
            // Generate rating based on click count
            rec.Rating =
                GenerateRealisticRating(rec.ClickCount, [], new Service { Id = rec.ServiceId });
            rec.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        logger.LogInformation("📊 Added {UnratedRecommendationsCount} additional ratings",
            unratedRecommendations.Count);
    }
}
