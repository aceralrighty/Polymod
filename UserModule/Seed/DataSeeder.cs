using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.MetricsModule.Services;
using TBD.ScheduleModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Seed;

public static class DataSeeder
{
    public static async Task ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var addressContext = scope.ServiceProvider.GetRequiredService<AddressDbContext>();

        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("UserModule");

        metricsService.IncrementCounter("seeding.user_db_delete_started");
        await userContext.Database.EnsureDeletedAsync();
        metricsService.IncrementCounter("seeding.user_db_delete_completed");

        metricsService.IncrementCounter("seeding.address_db_delete_started");
        await addressContext.Database.EnsureDeletedAsync();
        metricsService.IncrementCounter("seeding.address_db_delete_completed");

        metricsService.IncrementCounter("seeding.user_db_migration_started");
        await userContext.Database.MigrateAsync();
        metricsService.IncrementCounter("seeding.user_db_migration_completed");

        try
        {
            metricsService.IncrementCounter("seeding.address_db_migration_started");
            await addressContext.Database.MigrateAsync();
            metricsService.IncrementCounter("seeding.address_db_migration_completed");
        }
        catch (SqlException ex) when (ex.Number == 2714)
        {
            metricsService.IncrementCounter("seeding.address_db_migration_skipped_existing_tables");
            Console.WriteLine("Address context tables already exist, skipping migration");
        }

        await SeedUsersAsync(userContext, metricsService);
        metricsService.IncrementCounter("seeding.users_seeded");

        await SeedUserAddressesAsync(addressContext, userContext, metricsService);
        metricsService.IncrementCounter("seeding.user_addresses_seeded");
    }

    private static async Task SeedUsersAsync(UserDbContext context, IMetricsService metricsService)
    {
        var baseDate = DateTime.UtcNow;
        var hasher = new Hasher();

        var users = new List<User>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john.doe@example.com",
                Password = hasher.HashPassword("SecurePass123!"),
                CreatedAt = baseDate.AddDays(-365),
                UpdatedAt = baseDate.AddDays(-20),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "jane.smith",
                Email = "jane.smith@gmail.com",
                Password = hasher.HashPassword("MyPassword456$"),
                CreatedAt = baseDate.AddDays(-180),
                UpdatedAt = baseDate.AddDays(-10),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "admin.user",
                Email = "admin@company.org",
                Password = hasher.HashPassword("AdminSecure789#"),
                CreatedAt = baseDate.AddDays(-730),
                UpdatedAt = baseDate.AddDays(-1),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "maria.rodriguez",
                Email = "maria.rodriguez@outlook.com",
                Password = hasher.HashPassword("ContraseñaSegura321"),
                CreatedAt = baseDate.AddDays(-90),
                UpdatedAt = baseDate.AddDays(-5),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "wei.zhang",
                Email = "w.zhang@university.edu",
                Password = hasher.HashPassword("密码安全654"),
                CreatedAt = baseDate.AddDays(-45),
                UpdatedAt = baseDate.AddDays(-2),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "ahmad.hassan",
                Email = "ahmad.hassan@tech.ae",
                Password = hasher.HashPassword("SecureArabic987!"),
                CreatedAt = baseDate.AddDays(-120),
                UpdatedAt = baseDate.AddDays(-15),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "test.user.with.long.name",
                Email = "very.long.email.address.for.testing@extremelylongdomainname.international",
                Password = hasher.HashPassword("VeryLongPasswordWithSpecialChars!@#$%^&*()"),
                CreatedAt = baseDate.AddMinutes(-30),
                UpdatedAt = baseDate.AddMinutes(-30),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "a",
                Email = "a@b.co",
                Password = hasher.HashPassword("Short1!"),
                CreatedAt = baseDate.AddDays(-1),
                UpdatedAt = baseDate.AddDays(-1),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "user_2024",
                Email = "user+tag@example-domain.com",
                Password = hasher.HashPassword("Password!2024"),
                CreatedAt = baseDate.AddDays(-60),
                UpdatedAt = baseDate.AddDays(-30),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "test-user123",
                Email = "test.email+filter@subdomain.example.org",
                Password = hasher.HashPassword("Complex@Pass#123"),
                CreatedAt = baseDate.AddDays(-15),
                UpdatedAt = baseDate.AddDays(-7),
                Schedule = new Schedule()
            }
        };

        await context.Set<User>().AddRangeAsync(users);
        await context.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.users_seeded_successfully");
        metricsService.IncrementCounter($"seeding.users_total_{users.Count}");

        Console.WriteLine($"Seeded {users.Count} users");
    }

    private static async Task SeedUserAddressesAsync(AddressDbContext addressContext, UserDbContext context,
        IMetricsService metricsService)
    {
        var users = await context.Set<User>().ToListAsync();
        if (users.Count == 0)
        {
            Console.WriteLine("No users found for address seeding");
            metricsService.IncrementCounter("seeding.user_addresses_skipped_no_users");
            return;
        }

        var addresses = new List<UserAddress>
        {
            new(users[0].Id, users[0], "123 Main St", "Apt 1", "New York", "NY", "10001") { Id = Guid.NewGuid() },
            new(users[1].Id, users[1], "456 Oak Ave", null, "Boston", "MA", "02108") { Id = Guid.NewGuid() },
            new(users[2].Id, users[2], "789 Pine Rd", "Suite 300", "Chicago", "IL", "60601") { Id = Guid.NewGuid() }
        };

        await addressContext.UserAddress.AddRangeAsync(addresses);
        await addressContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.user_addresses_seeded_successfully");
        metricsService.IncrementCounter($"seeding.user_addresses_total_{addresses.Count}");

        Console.WriteLine($"Seeded {addresses.Count} addresses for {users.Count} users");
    }
}
