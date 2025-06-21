using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ScheduleModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Seed;

public static class DataSeeder
{
    public static async Task<List<User>> ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var addressContext = scope.ServiceProvider.GetRequiredService<AddressDbContext>();

        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("UserModule");

        try
        {
            Console.WriteLine("🔄 Starting user database operations...");

            // Check connection strings
            Console.WriteLine($"👥 User DB Connection: {userContext.Database.GetConnectionString()}");
            Console.WriteLine($"📍 Address DB Connection: {addressContext.Database.GetConnectionString()}");

            // Check if databases can be connected to before deletion
            var userDbExists = await userContext.Database.CanConnectAsync();
            var addressDbExists = await addressContext.Database.CanConnectAsync();
            Console.WriteLine($"👥 User database accessible before deletion: {userDbExists}");
            Console.WriteLine($"📍 Address database accessible before deletion: {addressDbExists}");

            metricsService.IncrementCounter("seeding.user_db_delete_started");
            await userContext.Database.EnsureDeletedAsync();
            Console.WriteLine("🗑️ User database deleted");
            metricsService.IncrementCounter("seeding.user_db_delete_completed");

            metricsService.IncrementCounter("seeding.address_db_delete_started");
            await addressContext.Database.EnsureDeletedAsync();
            Console.WriteLine("🗑️ Address database deleted");
            metricsService.IncrementCounter("seeding.address_db_delete_completed");

            metricsService.IncrementCounter("seeding.user_db_migration_started");
            await userContext.Database.MigrateAsync();
            Console.WriteLine("📊 User database migrated");
            metricsService.IncrementCounter("seeding.user_db_migration_completed");

            try
            {
                metricsService.IncrementCounter("seeding.address_db_migration_started");
                await addressContext.Database.MigrateAsync();
                Console.WriteLine("📊 Address database migrated");
                metricsService.IncrementCounter("seeding.address_db_migration_completed");
            }
            catch (SqlException ex) when (ex.Number == 2714)
            {
                metricsService.IncrementCounter("seeding.address_db_migration_skipped_existing_tables");
                Console.WriteLine("⚠️ Address context tables already exist, skipping migration");
            }

            // Verify databases exist after migration
            var userDbExistsAfter = await userContext.Database.CanConnectAsync();
            var addressDbExistsAfter = await addressContext.Database.CanConnectAsync();
            Console.WriteLine($"👥 User database accessible after migration: {userDbExistsAfter}");
            Console.WriteLine($"📍 Address database accessible after migration: {addressDbExistsAfter}");

            var seededUsers = await SeedUsersAsync(userContext, metricsService);
            Console.WriteLine($"✅ Successfully seeded {seededUsers.Count} users");
            metricsService.IncrementCounter("seeding.users_seeded");

            // Verify users were actually saved
            var userCountAfterSeeding = await userContext.Set<User>().CountAsync();
            Console.WriteLine($"🔢 User count in database after seeding: {userCountAfterSeeding}");

            if (userCountAfterSeeding != seededUsers.Count)
            {
                Console.WriteLine(
                    $"⚠️ WARNING: Expected {seededUsers.Count} users but found {userCountAfterSeeding} in database!");
            }

            // List the first few users for verification
            var usersInDb = await userContext.Set<User>().Take(3).Select(u => new { u.Id, u.Username }).ToListAsync();
            Console.WriteLine("📝 Users in database:");
            foreach (var user in usersInDb)
            {
                Console.WriteLine($"   👤 {user.Username} (ID: {user.Id})");
            }

            await SeedUserAddressesAsync(addressContext, userContext, metricsService);
            metricsService.IncrementCounter("seeding.user_addresses_seeded");
            return seededUsers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in DataSeeder: {ex.Message}");
            Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    private static async Task<List<User>> SeedUsersAsync(UserDbContext context, IMetricsService metricsService)
    {
        Console.WriteLine("🌱 Starting user seeding...");

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
            }
        };

        Console.WriteLine($"📝 Creating {users.Count} users...");

        foreach (var user in users.Take(3))
        {
            Console.WriteLine($"   👤 {user.Username} (ID: {user.Id})");
        }

        try
        {
            await context.Set<User>().AddRangeAsync(users);
            var saveResult = await context.SaveChangesAsync();
            Console.WriteLine($"💾 SaveChanges returned: {saveResult}");

            metricsService.IncrementCounter("seeding.users_seeded_successfully");
            metricsService.IncrementCounter($"seeding.users_total_{users.Count}");

            // Verify the save worked
            var countAfterSave = await context.Set<User>().CountAsync();
            Console.WriteLine($"🔢 Users in database after SaveChanges: {countAfterSave}");

            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving users: {ex.Message}");
            Console.WriteLine($"🔍 Inner exception: {ex.InnerException?.Message}");
            throw;
        }
    }

    private static async Task SeedUserAddressesAsync(AddressDbContext addressContext, UserDbContext context,
        IMetricsService metricsService)
    {
        Console.WriteLine("🌱 Starting address seeding...");

        var users = await context.Set<User>().ToListAsync();
        Console.WriteLine($"👥 Found {users.Count} users for address seeding");

        if (users.Count == 0)
        {
            Console.WriteLine("❌ No users found for address seeding");
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
        var savedAddresses = await addressContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.user_addresses_seeded_successfully");
        metricsService.IncrementCounter($"seeding.user_addresses_total_{addresses.Count}");

        Console.WriteLine($"✅ Seeded {savedAddresses} addresses for {users.Count} users");
    }
}
