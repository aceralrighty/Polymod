using Microsoft.EntityFrameworkCore;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ServiceModule.Data;
using TBD.ServiceModule.Models;

namespace TBD.ServiceModule.Seed;

public static class ServiceSeeder
{
    public static async Task<List<Service>> ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var serviceContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("ServiceModule");

        metricsService.IncrementCounter("seeding.service_database_recreate_started");

        await serviceContext.Database.EnsureDeletedAsync();
        await serviceContext.Database.MigrateAsync();

        metricsService.IncrementCounter("seeding.service_database_recreate_completed");

        var seededServices = await SeedServiceAsync(serviceContext, metricsService);
        await serviceContext.SaveChangesAsync();
        metricsService.IncrementCounter("seeding.service_full_reseed_completed");
        return seededServices;
    }

    private static async Task<List<Service>> SeedServiceAsync(ServiceDbContext serviceContext,
        IMetricsService metricsService)
    {
        metricsService.IncrementCounter("seeding.service_seed_started");

        var services = new List<Service>();
        var random = new Random(42); // Fixed seed for consistent test data
        var baseDate = DateTime.UtcNow.AddYears(-2); // Started 2 years ago

        // Helper method to generate realistic creation dates
        DateTime GetRandomCreatedAt(int monthsAgoMin, int monthsAgoMax)
        {
            var daysAgo = random.Next(monthsAgoMin * 30, monthsAgoMax * 30);
            return baseDate.AddDays(daysAgo);
        }

        // Helper method to determine if a service should be deleted (10% chance)
        DateTime? GetRandomDeletedAt(DateTime createdAt)
        {
            if (random.Next(1, 11) != 1) // 10% chance of being deleted
            {
                return null;
            }

            var daysSinceCreated = (DateTime.UtcNow - createdAt).Days;
            if (daysSinceCreated <= 30) // Only delete services older than 30 days
            {
                return null;
            }

            var deletedDaysAgo = random.Next(1, daysSinceCreated - 30);
            return DateTime.UtcNow.AddDays(-deletedDaysAgo);
        }

        var serviceData = new[]
        {
            new { Title = "Pilates", Description = "Get your body fit with Pilates", Price = 40m, Duration = 45 },
            new { Title = "Yoga", Description = "Get your body fit with Yoga", Price = 37m, Duration = 30 },
            new { Title = "Zumba", Description = "Get your body fit with Zumba", Price = 50m, Duration = 15 },
            new { Title = "Boxing", Description = "Get your body fit with Boxing", Price = 30m, Duration = 60 },
            new { Title = "Spinning", Description = "Get your body fit with Spinning", Price = 20m, Duration = 61 },
            new
            {
                Title = "Peloton",
                Description = "Catch the vibes with our special, inviting instructors! Only at Peloton",
                Price = 50m,
                Duration = 120
            },
            new
            {
                Title = "CrossFit",
                Description = "High-intensity functional movement training for maximum results",
                Price = 65m,
                Duration = 50
            },
            new
            {
                Title = "Hot Yoga",
                Description = "Traditional yoga in a heated room for deeper stretches and detox",
                Price = 45m,
                Duration = 75
            },
            new
            {
                Title = "HIIT Training",
                Description = "High-Intensity Interval Training for quick fat burning",
                Price = 55m,
                Duration = 40
            },
            new
            {
                Title = "Barre",
                Description = "Ballet-inspired workout combining strength, flexibility, and cardio",
                Price = 42m,
                Duration = 55
            },
            new
            {
                Title = "Aqua Aerobics",
                Description = "Low-impact water-based fitness for all ages and abilities",
                Price = 35m,
                Duration = 45
            },
            new
            {
                Title = "Kickboxing",
                Description = "Combat sport training for cardio, strength, and self-defense",
                Price = 48m,
                Duration = 60
            },
            new
            {
                Title = "TRX Suspension",
                Description = "Bodyweight training using suspension straps for full-body workout",
                Price = 52m,
                Duration = 45
            },
            new
            {
                Title = "Dance Fitness",
                Description = "Fun cardio workout combining popular dance moves and music",
                Price = 38m,
                Duration = 50
            },
            new
            {
                Title = "Strength Training",
                Description = "Weight lifting and resistance training for muscle building",
                Price = 60m,
                Duration = 75
            },
            new
            {
                Title = "Meditation & Mindfulness",
                Description = "Guided meditation sessions for mental wellness and stress relief",
                Price = 25m,
                Duration = 30
            },
            new
            {
                Title = "Rock Climbing",
                Description = "Indoor rock climbing for beginners to advanced climbers",
                Price = 75m,
                Duration = 90
            },
            new
            {
                Title = "Tai Chi",
                Description = "Ancient Chinese martial art focusing on slow, flowing movements",
                Price = 32m,
                Duration = 60
            },
            new
            {
                Title = "Bootcamp",
                Description = "Military-style group fitness with varied high-intensity exercises",
                Price = 58m,
                Duration = 55
            },
            new
            {
                Title = "Personal Training",
                Description = "One-on-one customized fitness coaching tailored to your goals",
                Price = 95m,
                Duration = 60
            },
            new
            {
                Title = "Stretching & Recovery",
                Description = "Assisted stretching and mobility work for injury prevention",
                Price = 40m,
                Duration = 45
            },
            new
            {
                Title = "Functional Movement",
                Description = "Movement patterns that improve daily life activities and mobility",
                Price = 46m,
                Duration = 50
            },
            new
            {
                Title = "Senior Fitness",
                Description = "Low-impact exercise program designed specifically for seniors",
                Price = 28m,
                Duration = 40
            },
            new
            {
                Title = "Prenatal Yoga",
                Description = "Safe yoga practice modified for expecting mothers",
                Price = 43m,
                Duration = 50
            },
            new
            {
                Title = "Martial Arts",
                Description = "Traditional martial arts training including karate and judo",
                Price = 55m,
                Duration = 75
            },

            // Additional new services
            new
            {
                Title = "Aerial Yoga",
                Description = "Yoga practice using silk hammocks for support and deeper poses",
                Price = 48m,
                Duration = 60
            },
            new
            {
                Title = "Calisthenics",
                Description = "Bodyweight exercises for strength and muscle development",
                Price = 35m,
                Duration = 45
            },
            new
            {
                Title = "Foam Rolling Recovery",
                Description = "Self-myofascial release techniques for muscle recovery",
                Price = 22m,
                Duration = 30
            },
            new
            {
                Title = "Olympic Weightlifting",
                Description = "Technical training in snatch and clean & jerk movements",
                Price = 85m,
                Duration = 90
            },
            new
            {
                Title = "Parkour",
                Description = "Movement discipline focusing on navigating obstacles efficiently",
                Price = 55m,
                Duration = 75
            },
            new
            {
                Title = "Pole Dancing Fitness",
                Description = "Strength and flexibility training using vertical poles",
                Price = 50m,
                Duration = 60
            },
            new
            {
                Title = "Rowing Training",
                Description = "Full-body cardio workout using rowing machines and technique",
                Price = 42m,
                Duration = 45
            },
            new
            {
                Title = "Breathwork Sessions",
                Description = "Therapeutic breathing techniques for stress relief and wellness",
                Price = 35m,
                Duration = 45
            },
            new
            {
                Title = "Nutrition Coaching",
                Description = "Personalized dietary guidance and meal planning",
                Price = 70m,
                Duration = 60
            },
            new
            {
                Title = "Movement Therapy",
                Description = "Therapeutic exercise for injury rehabilitation and prevention",
                Price = 65m,
                Duration = 50
            },
            new
            {
                Title = "Suspension Yoga",
                Description = "Yoga combined with aerial silk support for enhanced flexibility",
                Price = 47m,
                Duration = 60
            },
            new
            {
                Title = "Battle Ropes",
                Description = "High-intensity cardio training using heavy rope exercises",
                Price = 38m,
                Duration = 30
            },
            new
            {
                Title = "Mobility & Flexibility",
                Description = "Joint mobility and flexibility improvement sessions",
                Price = 33m,
                Duration = 45
            },
            new
            {
                Title = "Kettlebell Training",
                Description = "Functional strength training using kettlebell movements",
                Price = 45m,
                Duration = 50
            },
            new
            {
                Title = "Swim Training",
                Description = "Technique improvement and endurance for swimmers",
                Price = 52m,
                Duration = 60
            },
            new
            {
                Title = "Wellness Coaching",
                Description = "Holistic approach to health including lifestyle and mindset",
                Price = 80m,
                Duration = 75
            },
            new
            {
                Title = "Virtual Reality Fitness",
                Description = "Immersive fitness gaming experience for cardio and fun",
                Price = 35m,
                Duration = 30
            },
            new
            {
                Title = "Posture Correction",
                Description = "Specialized training to improve posture and reduce pain",
                Price = 55m,
                Duration = 45
            },
            new
            {
                Title = "Sports Conditioning",
                Description = "Sport-specific training for athletic performance enhancement",
                Price = 75m,
                Duration = 90
            },
            new
            {
                Title = "Mindful Movement",
                Description = "Gentle movement practice combining mindfulness and mobility",
                Price = 30m,
                Duration = 40
            },

            // Some legacy/discontinued services (will be marked as deleted)
            new
            {
                Title = "Step Aerobics",
                Description = "Classic step-based cardio workout from the 90s",
                Price = 25m,
                Duration = 45
            },
            new
            {
                Title = "Jazzercise",
                Description = "Dance-based fitness combining jazz dance with exercise",
                Price = 28m,
                Duration = 50
            },
            new
            {
                Title = "Water Boxing",
                Description = "Boxing movements performed in shallow water",
                Price = 40m,
                Duration = 45
            },
            new
            {
                Title = "Chair Yoga",
                Description = "Modified yoga practice designed for seated participants",
                Price = 20m,
                Duration = 30
            },
            new
            {
                Title = "Balance Training",
                Description = "Specialized training to improve stability and prevent falls",
                Price = 35m,
                Duration = 40
            }
        };

        foreach (var (data, _) in serviceData.Select((data, index) => (data, index)))
        {
            var createdAt = GetRandomCreatedAt(0, 24); // Created sometime in the last 2 years
            var deletedAt = GetRandomDeletedAt(createdAt);

            var service = new Service
            {
                Title = data.Title,
                Description = data.Description,
                Price = data.Price,
                DurationInMinutes = data.Duration,
                ProviderId = Guid.NewGuid(),
                CreatedAt = createdAt,
                UpdatedAt = createdAt.AddDays(random.Next(0, (DateTime.UtcNow - createdAt).Days + 1)),
                DeletedAt = deletedAt
            };

            services.Add(service);
        }

        // Sort services by creation date for a more realistic database state
        services = services.OrderBy(s => s.CreatedAt).ToList();

        // Track service metrics by categories
        var activeServices = services.Where(s => s.DeletedAt == null).ToList();
        var deletedServices = services.Where(s => s.DeletedAt != null).ToList();
        var fitnessServices = activeServices.Count(s => IsFitnessService(s.Title));
        var wellnessServices = activeServices.Count(s => IsWellnessService(s.Title));
        var premiumServices = activeServices.Count(s => s.Price >= 60);
        var quickServices = activeServices.Count(s => s.DurationInMinutes <= 45);
        var longServices = activeServices.Count(s => s.DurationInMinutes >= 75);

        // Log metrics
        for (var i = 0; i < services.Count; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_total");
        }

        for (var i = 0; i < deletedServices.Count; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_deleted");
        }

        for (var i = 0; i < fitnessServices; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_fitness");
        }

        for (var i = 0; i < wellnessServices; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_wellness");
        }

        for (var i = 0; i < premiumServices; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_premium");
        }

        for (var i = 0; i < quickServices; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_quick");
        }

        for (var i = 0; i < longServices; i++)
        {
            metricsService.IncrementCounter("seeding.services_created_long");
        }

        await serviceContext.Services.AddRangeAsync(services);
        await serviceContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.service_database_save_completed");
        metricsService.IncrementCounter("seeding.service_seed_completed");

        Console.WriteLine($"Seeded {services.Count} services total:");
        Console.WriteLine($"  - Active: {activeServices.Count}");
        Console.WriteLine($"  - Deleted: {deletedServices.Count}");
        Console.WriteLine($"  - Fitness: {fitnessServices}");
        Console.WriteLine($"  - Wellness: {wellnessServices}");
        Console.WriteLine($"  - Premium (≥$60): {premiumServices}");

        return services;
    }

    private static bool IsFitnessService(string title)
    {
        var fitnessKeywords = new[]
        {
            "CrossFit", "HIIT", "Boxing", "Kickboxing", "Bootcamp", "Strength", "TRX", "Spinning", "Calisthenics",
            "Olympic", "Parkour", "Rowing", "Battle Ropes", "Kettlebell", "Sports"
        };
        return fitnessKeywords.Any(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsWellnessService(string title)
    {
        var wellnessKeywords = new[]
        {
            "Yoga", "Meditation", "Tai Chi", "Stretching", "Prenatal", "Breathwork", "Mindfulness", "Wellness",
            "Movement Therapy", "Mindful"
        };
        return wellnessKeywords.Any(keyword => title.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }
}
