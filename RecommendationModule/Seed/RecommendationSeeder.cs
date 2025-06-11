using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.RecommendationModule.Data;
using TBD.RecommendationModule.Models;
using TBD.ServiceModule.Models;
using TBD.UserModule.Models;

namespace TBD.RecommendationModule.Seed;

public static class RecommendationSeeder
{
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider, List<User> users, List<Service> services)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecommendationDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("RecommendationModule");

        try
        {
            Console.WriteLine("🔄 Starting recommendation database operations...");

            metricsService.IncrementCounter("seeding.recommendation_database_recreate_started");

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            metricsService.IncrementCounter("seeding.recommendation_database_recreate_completed");

            if (users == null || users.Count == 0)
            {
                Console.WriteLine("❌ No users provided for recommendation seeding");
                throw new InvalidOperationException("Cannot seed recommendations without users");
            }

            if (services == null || !services.Any())
            {
                Console.WriteLine("❌ No services provided for recommendation seeding");
                throw new InvalidOperationException("Cannot seed recommendations without services");
            }

            Console.WriteLine($"👥 Using {users.Count} users for recommendations");
            Console.WriteLine($"🎯 Using {services.Count} services for recommendations");

            // 👇 Add users to this context first
            await context.Users.AddRangeAsync(users);
            await context.SaveChangesAsync();
            Console.WriteLine($"👥 Added {users.Count} users to recommendation context");

            // 👇 Ensure services are saved in this context before referencing their IDs
            await context.Services.AddRangeAsync(services);
            await context.SaveChangesAsync();
            Console.WriteLine($"🎯 Added {services.Count} services to recommendation context");

            await SeedRecommendationAsync(context, metricsService, users, services);
            metricsService.IncrementCounter("seeding.recommendation_full_reseed_completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in RecommendationSeeder: {ex.Message}");
            Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static async Task SeedRecommendationAsync(RecommendationDbContext context, IMetricsService metricsService, List<User> users, List<Service> services)
    {
        metricsService.IncrementCounter("seeding.recommendation_seed_started");

        var recommendations = new List<UserRecommendation>();
        var random = new Random();

        foreach (var user in users)
        {
            var numberOfRecommendations = random.Next(3, 9);

            var recommendedServices = services
                .OrderBy(_ => Guid.NewGuid())
                .Take(numberOfRecommendations)
                .ToList();

            foreach (var service in recommendedServices)
            {
                var recommendation = new UserRecommendation
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    ServiceId = service.Id,
                    RecommendedAt = DateTime.UtcNow.AddDays(-random.Next(0, 30)),
                    ClickCount = random.Next(0, 5),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                recommendations.Add(recommendation);

                if (recommendations.Count <= 5)
                {
                    Console.WriteLine($"   💡 Recommendation: User {user.Username} -> Service {service.Title}");
                }
            }
        }

        Console.WriteLine($"📝 Creating {recommendations.Count} recommendations...");

        await context.UserRecommendations.AddRangeAsync(recommendations);
        var savedCount = await context.SaveChangesAsync();
        Console.WriteLine($"💾 Saved {savedCount} recommendations to database");

        metricsService.IncrementCounter("seeding.recommendations_created_total");
        foreach (var _ in recommendations)
        {
            metricsService.IncrementCounter("seeding.recommendation_created");
        }

        var countAfterSave = await context.UserRecommendations.CountAsync();
        Console.WriteLine($"🔢 Recommendations in database after SaveChanges: {countAfterSave}");

        metricsService.IncrementCounter("seeding.recommendation_database_save_completed");
        metricsService.IncrementCounter("seeding.recommendation_seed_completed");
    }
}
