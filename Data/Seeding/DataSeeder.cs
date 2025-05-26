using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.ScheduleModule.Models;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.Data.Seeding;

public static class DataSeeder
{
  
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var addressContext = scope.ServiceProvider.GetRequiredService<AddressDbContext>();

        // ⚠️ Drop and recreate the DBs - only do this for local testing!
        await userContext.Database.EnsureDeletedAsync();
        await addressContext.Database.EnsureDeletedAsync();

        await userContext.Database.MigrateAsync();
        await addressContext.Database.MigrateAsync();

        await SeedUsersAsync(userContext);
        await SeedUserAddressesAsync(addressContext, userContext);
    }

    
    private static async Task SeedUsersAsync(UserDbContext context)
    {
        var users = new List<User>
        {
            new User
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john.doe@example.com",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow - TimeSpan.FromDays(20),
                Schedule = new Schedule()
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "jane.smith",
                Email = "jane.smith@example.com",
                CreatedAt = DateTime.Today - TimeSpan.FromDays(30),
                UpdatedAt = DateTime.UtcNow - TimeSpan.FromDays(10),
                Schedule = new Schedule()
            },
            new User
            {
                Id = Guid.NewGuid(),
                Username = "admin.user",
                Email = "admin@example.com",
                CreatedAt = DateTime.Today - TimeSpan.FromDays(20),
                UpdatedAt = DateTime.Today - TimeSpan.FromDays(5),
                Schedule = new Schedule()
            }
        };

        await context.Set<User>().AddRangeAsync(users);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {users.Count} users");
    }

    private static async Task SeedUserAddressesAsync(AddressDbContext addressContext, UserDbContext context)
    {
        var users = await context.Set<User>().ToListAsync();
        if (users.Count == 0)
        {
            Console.WriteLine("No users found for address seeding");
            return;
        }

        var addresses = new List<UserAddress>();

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

        await addressContext.UserAddress.AddRangeAsync(addresses);
        await addressContext.SaveChangesAsync();
    }
}
