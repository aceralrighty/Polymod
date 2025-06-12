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
            var users = await GetSampleUsersAsync(serviceProvider);
            var services = await GetSampleServicesAsync(serviceProvider);


            var logger = serviceProvider.GetRequiredService<ILogger<RecommendationSeederAndTrainer>>();
            var seeder = new RecommendationSeederAndTrainer(serviceProvider, logger);

            await seeder.SeedAndTrainAsync(users, services, includeRatings: true);

            // Option 2: Just seed the database
            await seeder.SeedRecommendationsWithRatingsAsync(users, services, includeRatings: true);

            // Option 3: Just train the model (if data already exists)
            // await seeder.TrainRecommendationModelAsync();

            // Option 4: Add more ratings to existing data
            // await seeder.AddAdditionalRatingsAsync(500);
        }

        private static Task<List<User>> GetSampleUsersAsync(IServiceProvider serviceProvider)
        {
            var hashedPassword = new Hasher();
            return Task.FromResult<List<User>>([
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "john_doe",
                    Email = "john@example.com",
                    Password = hashedPassword.HashPassword("Sinners"),
                    Schedule = new Schedule()
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "jane_smith",
                    Email = "jane@example.com",
                    Password = hashedPassword.HashPassword("Boogaloo"),
                    Schedule = new Schedule()
                },
                new User
                {
                    Id = Guid.NewGuid(),
                    Username = "bob_wilson",
                    Email = "bob@example.com",
                    Password = hashedPassword.HashPassword("why lord why"),
                    Schedule = new Schedule()
                },
                // Add more users as needed
            ]);
        }

        private static Task<List<Service>> GetSampleServicesAsync(IServiceProvider serviceProvider)
        {
            // In real implementation, get from your service repository
            // For now, return sample services
            return Task.FromResult(new List<Service>
            {
                new() { Id = Guid.NewGuid(), Title = "Cloud Storage Service", Description = "Secure cloud storage" },
                new()
                {
                    Id = Guid.NewGuid(), Title = "Video Streaming", Description = "Entertainment streaming platform"
                },
                new() { Id = Guid.NewGuid(), Title = "Online Learning", Description = "Educational courses online" },
                // Add more services as needed
            });
        }
    }

// Extension method for easy DI registration
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRecommendationSeeder(IServiceCollection services)
        {
            services.AddScoped<RecommendationSeederAndTrainer>();
            return services;
        }
    }

    /// <summary>
    /// Complete workflow: Seed database with realistic data and train ML model
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
    public async Task SeedRecommendationsWithRatingsAsync(List<User> users, List<Service> services,
        bool includeRatings = true)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("RecommendationSeeder");

        try
        {
            logger.LogInformation("🔄 Starting recommendation database seeding...");

            // Recreate database
            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            // Add users and services to context
            await context.Users.AddRangeAsync(users);
            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();

            logger.LogInformation($"👥 Added {users.Count} users and 🎯 {services.Count} services to context");

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
    public async Task TrainRecommendationModelAsync()
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
                    $"👤 User {user.Username}: Generated {mlRecommendations.Count()} ML recommendations");

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
                logger.LogWarning(ex, $"⚠️ Validation issue for user {user.Username}");
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
        float baseRating = clickCount switch
        {
            0 => GenerateRatingInRange(1.0f, 3.0f), // Low rating if no clicks
            1 => GenerateRatingInRange(2.0f, 4.0f), // Mixed rating for single click
            >= 2 and <= 4 => GenerateRatingInRange(3.0f, 5.0f), // Good rating for moderate engagement
            >= 5 and <= 8 => GenerateRatingInRange(3.5f, 5.0f), // High rating for high engagement
            _ => GenerateRatingInRange(4.0f, 5.0f) // Excellent rating for very high engagement
        };

        // Adjust rating based on user preferences (simulate user liking certain service types more)
        var serviceCategory = GetServiceCategory(service);
        if (userPreferences.Contains(serviceCategory))
        {
            baseRating = Math.Min(5.0f, baseRating + _random.Next(0, 10) * 0.1f); // Boost for preferred category
        }

        // Add some randomness but keep it realistic
        var noise = (_random.NextSingle() - 0.5f) * 0.4f; // ±0.2 rating noise
        baseRating = Math.Max(1.0f, Math.Min(5.0f, baseRating + noise));

        // Round to nearest 0.5 for realistic ratings
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

        // Simple categorization based on service index (in real app, you'd use actual categories)
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
            .SelectMany(pref => categories.ContainsKey(pref) ? categories[pref] : new List<Service>())
            .Distinct()
            .OrderBy(_ => _random.Next())
            .Take(preferredCount);

        selectedServices.AddRange(preferredServices);

        // Fill remaining with random services
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

        logger.LogInformation($"📊 Seeded Data Statistics:");
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
                GenerateRealisticRating(rec.ClickCount, new List<string>(), new Service { Id = rec.ServiceId });
            rec.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        logger.LogInformation($"📊 Added {unratedRecommendations.Count} additional ratings");
    }
}
