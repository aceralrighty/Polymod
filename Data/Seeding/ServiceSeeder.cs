using TBD.ServiceModule.Data;
using Microsoft.EntityFrameworkCore;
using TBD.ServiceModule.Models;

namespace TBD.Data.Seeding;

public class ServiceSeeder
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
            Price = 20,
            DurationInMinutes = 60,
            ProviderId = Guid.NewGuid()
        };
        Console.WriteLine($"This costs {service1.FormattedPrice}");


        var service2 = new Service
        {
            Id = Guid.NewGuid(),
            Title = "Yoga",
            Description = "Get your body fit with Yoga",
            Price = 20,
            DurationInMinutes = 60,
            ProviderId = Guid.NewGuid()
        };
        var service3 = new Service
        {
            Id = Guid.NewGuid(),
            Title = "Zumba",
            Description = "Get your body fit with Zumba",
            Price = 20,
            DurationInMinutes = 60,
            ProviderId = Guid.NewGuid()
        };
        Console.WriteLine($"This costs {service3.FormattedPrice}");

        services.Add(service1);
        services.Add(service2);
        services.Add(service3);
        await serviceContext.Services.AddRangeAsync(services);
        await serviceContext.SaveChangesAsync();
    }
}