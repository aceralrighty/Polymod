using Microsoft.EntityFrameworkCore;
using TBD.Models.Entities;

namespace TBD.Data.Seeding;

public class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GenericDatabaseContext>();

        await context.Database.MigrateAsync();
    
        // Call to seed users first
        await SeedUsersAsync(context);
    
        // Then seed addresses
        await SeedUserAddressesAsync(context);
    }

    private static async Task SeedUsersAsync(GenericDatabaseContext context)
    {
        // Check if there are already users in the database
        if (await context.Set<User>().AnyAsync())
        {
            return; // Skip seeding if data already exists
        }

        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "jane.smith",
                Email = "jane.smith@example.com",
                CreatedAt = DateTime.Today - TimeSpan.FromDays(10),
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "admin.user",
                Email = "admin@example.com",
                CreatedAt = DateTime.Today - TimeSpan.FromDays(20),
            }
        };

        await context.Set<User>().AddRangeAsync(users);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {users.Count} users");
    }

    private static async Task SeedUserAddressesAsync(GenericDatabaseContext context)
    {
        // Check if there are already addresses in the database
        if (await context.Set<UserAddress>().AnyAsync())
        {
            return; // Skip seeding if data already exists
        }

        // Get users to associate addresses with
        var users = await context.Set<User>().ToListAsync();
        if (!users.Any())
        {
            Console.WriteLine("No users found for address seeding");
            return;
        }

        var addresses = new List<UserAddress>();

        // Add address for first user
        if (users.Count > 0)
        {
            addresses.Add(new UserAddress(
                userId: users[0].Id,
                user: users[0],
                address1: "123 Main Street",
                address2: "Apt 101",
                city: "Seattle",
                state: "WA",
                zipCode: 98101)
            {
                Id = Guid.NewGuid()
            });
        }

        // Add address for second user
        if (users.Count > 1)
        {
            addresses.Add(new UserAddress(
                userId: users[1].Id,
                user: users[1],
                address1: "456 Market Street",
                address2: null,
                city: "San Francisco",
                state: "CA",
                zipCode: 94103)
            {
                Id = Guid.NewGuid()
            });
        }

        // Add address for third user
        if (users.Count > 2)
        {
            addresses.Add(new UserAddress(
                userId: users[2].Id,
                user: users[2],
                address1: "789 Broadway",
                address2: "Suite 200",
                city: "New York",
                state: "NY",
                zipCode: 10001)
            {
                Id = Guid.NewGuid()
            });
        }

        await context.Set<UserAddress>().AddRangeAsync(addresses);
        await context.SaveChangesAsync();
    }
}