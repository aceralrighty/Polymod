using TBD.ServiceModule.Data;
using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Models;

namespace TBD.Data.Seeding;

public static class ServiceSeeder
{
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var serviceContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();


        await serviceContext.Database.EnsureDeletedAsync();
        await serviceContext.Database.MigrateAsync();

        await SeedServiceAsync(serviceContext);
    }

    private static async Task SeedServiceAsync(ServiceDbContext serviceContext)
{
    var services = new List<Service>();

    var service1 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Pilates",
        Description = "Get your body fit with Pilates",
        Price = 40,
        DurationInMinutes = 45,
        ProviderId = Guid.NewGuid()
    };

    var service2 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Yoga",
        Description = "Get your body fit with Yoga",
        Price = 37,
        DurationInMinutes = 30,
        ProviderId = Guid.NewGuid()
    };

    var service3 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Zumba",
        Description = "Get your body fit with Zumba",
        Price = 50,
        DurationInMinutes = 15,
        ProviderId = Guid.NewGuid()
    };

    var service4 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Boxing",
        Description = "Get your body fit with Boxing",
        Price = 30,
        DurationInMinutes = 60,
        ProviderId = Guid.NewGuid()
    };

    var service5 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Spinning",
        Description = "Get your body fit with Spinning",
        Price = 20,
        DurationInMinutes = 61,
        ProviderId = Guid.NewGuid()
    };

    var service6 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Peloton",
        Description = "Catch the vibes with our special, inviting instructors! Only at Peloton",
        Price = 50,
        DurationInMinutes = 120,
        ProviderId = Guid.NewGuid()
    };

    var service7 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "CrossFit",
        Description = "High-intensity functional movement training for maximum results",
        Price = 65,
        DurationInMinutes = 50,
        ProviderId = Guid.NewGuid()
    };

    var service8 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Hot Yoga",
        Description = "Traditional yoga in a heated room for deeper stretches and detox",
        Price = 45,
        DurationInMinutes = 75,
        ProviderId = Guid.NewGuid()
    };

    var service9 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "HIIT Training",
        Description = "High-Intensity Interval Training for quick fat burning",
        Price = 55,
        DurationInMinutes = 40,
        ProviderId = Guid.NewGuid()
    };

    var service10 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Barre",
        Description = "Ballet-inspired workout combining strength, flexibility, and cardio",
        Price = 42,
        DurationInMinutes = 55,
        ProviderId = Guid.NewGuid()
    };

    var service11 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Aqua Aerobics",
        Description = "Low-impact water-based fitness for all ages and abilities",
        Price = 35,
        DurationInMinutes = 45,
        ProviderId = Guid.NewGuid()
    };

    var service12 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Kickboxing",
        Description = "Combat sport training for cardio, strength, and self-defense",
        Price = 48,
        DurationInMinutes = 60,
        ProviderId = Guid.NewGuid()
    };

    var service13 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "TRX Suspension",
        Description = "Bodyweight training using suspension straps for full-body workout",
        Price = 52,
        DurationInMinutes = 45,
        ProviderId = Guid.NewGuid()
    };

    var service14 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Dance Fitness",
        Description = "Fun cardio workout combining popular dance moves and music",
        Price = 38,
        DurationInMinutes = 50,
        ProviderId = Guid.NewGuid()
    };

    var service15 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Strength Training",
        Description = "Weight lifting and resistance training for muscle building",
        Price = 60,
        DurationInMinutes = 75,
        ProviderId = Guid.NewGuid()
    };

    var service16 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Meditation & Mindfulness",
        Description = "Guided meditation sessions for mental wellness and stress relief",
        Price = 25,
        DurationInMinutes = 30,
        ProviderId = Guid.NewGuid()
    };

    var service17 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Rock Climbing",
        Description = "Indoor rock climbing for beginners to advanced climbers",
        Price = 75,
        DurationInMinutes = 90,
        ProviderId = Guid.NewGuid()
    };

    var service18 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Tai Chi",
        Description = "Ancient Chinese martial art focusing on slow, flowing movements",
        Price = 32,
        DurationInMinutes = 60,
        ProviderId = Guid.NewGuid()
    };

    var service19 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Bootcamp",
        Description = "Military-style group fitness with varied high-intensity exercises",
        Price = 58,
        DurationInMinutes = 55,
        ProviderId = Guid.NewGuid()
    };

    var service20 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Personal Training",
        Description = "One-on-one customized fitness coaching tailored to your goals",
        Price = 95,
        DurationInMinutes = 60,
        ProviderId = Guid.NewGuid()
    };

    var service21 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Stretching & Recovery",
        Description = "Assisted stretching and mobility work for injury prevention",
        Price = 40,
        DurationInMinutes = 45,
        ProviderId = Guid.NewGuid()
    };

    var service22 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Functional Movement",
        Description = "Movement patterns that improve daily life activities and mobility",
        Price = 46,
        DurationInMinutes = 50,
        ProviderId = Guid.NewGuid()
    };

    var service23 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Senior Fitness",
        Description = "Low-impact exercise program designed specifically for seniors",
        Price = 28,
        DurationInMinutes = 40,
        ProviderId = Guid.NewGuid()
    };

    var service24 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Prenatal Yoga",
        Description = "Safe yoga practice modified for expecting mothers",
        Price = 43,
        DurationInMinutes = 50,
        ProviderId = Guid.NewGuid()
    };

    var service25 = new Service
    {
        Id = Guid.NewGuid(),
        Title = "Martial Arts",
        Description = "Traditional martial arts training including karate and judo",
        Price = 55,
        DurationInMinutes = 75,
        ProviderId = Guid.NewGuid()
    };

    // Add all services to the list
    services.AddRange([
        service1, service2, service3, service4, service5, service6, service7, service8, service9, service10,
        service11, service12, service13, service14, service15, service16, service17, service18, service19, service20,
        service21, service22, service23, service24, service25
    ]);

    await serviceContext.Services.AddRangeAsync(services);
    await serviceContext.SaveChangesAsync();
}
}
