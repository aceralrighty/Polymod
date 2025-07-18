using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.MetricsModule.Services.Interfaces;
using TBD.ScheduleModule.Models;
using TBD.Shared.Utils;
using TBD.UserModule.Data;
using TBD.UserModule.Models;

namespace TBD.UserModule.Seed;

public static class DataSeeder
{
    private static readonly ActivitySource ActivitySource = new("TBD.UserModule.DataSeeder");

    public static async Task<List<User>> ReseedForTestingAsync(IServiceProvider serviceProvider)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.ReseedForTesting");
        activity?.SetTag("operation", "reseed_for_testing");

        using var scope = serviceProvider.CreateScope();

        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var addressContext = scope.ServiceProvider.GetRequiredService<AddressDbContext>();

        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("UserModule");

        try
        {
            Console.WriteLine("🔄 Starting user database operations...");

            var operationStartTime = DateTime.UtcNow;

            var userDbExists = await userContext.Database.CanConnectAsync();
            var addressDbExists = await addressContext.Database.CanConnectAsync();

            Console.WriteLine($"👥 User database accessible before deletion: {userDbExists}");
            Console.WriteLine($"📍 Address database accessible before deletion: {addressDbExists}");

            activity?.SetTag("user_db_accessible_before", userDbExists);
            activity?.SetTag("address_db_accessible_before", addressDbExists);

            await DeleteDatabasesAsync(userContext, addressContext, metricsService, activity);
            await MigrateDatabasesAsync(userContext, addressContext, metricsService, activity);

            var userDbExistsAfter = await userContext.Database.CanConnectAsync();
            var addressDbExistsAfter = await addressContext.Database.CanConnectAsync();

            Console.WriteLine($"👥 User database accessible after migration: {userDbExistsAfter}");
            Console.WriteLine($"📍 Address database accessible after migration: {addressDbExistsAfter}");

            activity?.SetTag("user_db_accessible_after", userDbExistsAfter);
            activity?.SetTag("address_db_accessible_after", addressDbExistsAfter);

            var seededUsers = await SeedUsersAsync(userContext, metricsService, activity);
            Console.WriteLine($"✅ Successfully seeded {seededUsers.Count} users");

            activity?.SetTag("users_seeded_count", seededUsers.Count);
            metricsService.IncrementCounter($"seeding.users_seeded -> {seededUsers.Count}");

            var userCountAfterSeeding = await userContext.Set<User>().CountAsync();
            Console.WriteLine($"🔢 User count in database after seeding: {userCountAfterSeeding}");

            if (userCountAfterSeeding != seededUsers.Count)
            {
                Console.WriteLine(
                    $"⚠️ WARNING: Expected {seededUsers.Count} users but found {userCountAfterSeeding} in database!");
                activity?.SetTag("users_count_mismatch", true);
                metricsService.IncrementCounter("seeding.users_count_mismatch");
            }

            var usersInDb = await userContext.Set<User>().Take(3).Select(u => new { u.Id, u.Username }).ToListAsync();
            Console.WriteLine("📝 Users in database:");
            foreach (var user in usersInDb)
            {
                Console.WriteLine($"   👤 {user.Username} (ID: {user.Id})");
            }

            await SeedUserAddressesAsync(addressContext, userContext, metricsService, activity);
            metricsService.IncrementCounter("seeding.user_addresses_seeded");

            var operationDuration = DateTime.UtcNow - operationStartTime;
            metricsService.RecordHistogram("seeding.total_operation_duration_seconds", operationDuration.TotalSeconds);

            activity?.SetTag("total_duration_seconds", operationDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return seededUsers;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error in DataSeeder: {ex.Message}");
            Console.WriteLine($"🔍 Stack trace: {ex.StackTrace}");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            metricsService.IncrementCounter("seeding.errors_total");
            metricsService.IncrementCounter($"seeding.errors_{ex.GetType().Name}");

            throw;
        }
    }

    private static async Task DeleteDatabasesAsync(
        UserDbContext userContext, AddressDbContext addressContext,
        IMetricsService metricsService, Activity? parentActivity)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.DeleteDatabases", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "delete_databases");

        try
        {
            var deletionStartTime = DateTime.UtcNow;

            metricsService.IncrementCounter("seeding.user_db_delete_started");
            await userContext.Database.EnsureDeletedAsync();
            metricsService.IncrementCounter("seeding.user_db_delete_completed");
            Console.WriteLine("🗑️ User database deleted");

            metricsService.IncrementCounter("seeding.address_db_delete_started");
            await addressContext.Database.EnsureDeletedAsync();
            metricsService.IncrementCounter("seeding.address_db_delete_completed");
            Console.WriteLine("🗑️ Address database deleted");

            var deletionDuration = DateTime.UtcNow - deletionStartTime;
            metricsService.RecordHistogram("seeding.database_deletion_duration_seconds", deletionDuration.TotalSeconds);
            activity?.SetTag("deletion_duration_seconds", deletionDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error deleting databases: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.database_deletion_errors");
            throw;
        }
    }

    private static async Task MigrateDatabasesAsync(
        UserDbContext userContext, AddressDbContext addressContext,
        IMetricsService metricsService, Activity? parentActivity)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.MigrateDatabases", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "migrate_databases");

        try
        {
            var migrationStartTime = DateTime.UtcNow;

            metricsService.IncrementCounter("seeding.user_db_migration_started");
            await userContext.Database.MigrateAsync();
            metricsService.IncrementCounter("seeding.user_db_migration_completed");
            Console.WriteLine("📊 User database migrated");

            try
            {
                metricsService.IncrementCounter("seeding.address_db_migration_started");
                await addressContext.Database.MigrateAsync();
                metricsService.IncrementCounter("seeding.address_db_migration_completed");
                Console.WriteLine("📊 Address database migrated");
            }
            catch (SqlException ex) when (ex.Number == 2714) // Table already exists
            {
                metricsService.IncrementCounter("seeding.address_db_migration_skipped_existing_tables");
                Console.WriteLine("⚠️ Address context tables already exist, skipping migration");
                activity?.SetTag("address_migration_skipped", true);
            }

            var migrationDuration = DateTime.UtcNow - migrationStartTime;
            metricsService.RecordHistogram("seeding.database_migration_duration_seconds",
                migrationDuration.TotalSeconds);
            activity?.SetTag("migration_duration_seconds", migrationDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error migrating databases: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.database_migration_errors");
            throw;
        }
    }

    private static async Task<List<User>> SeedUsersAsync(
        UserDbContext context, IMetricsService metricsService, Activity? parentActivity)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUsers", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_users");

        Console.WriteLine("🌱 Starting user seeding...");

        var seedingStartTime = DateTime.UtcNow;
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

        activity?.SetTag("users_to_create", users.Count);

        foreach (var user in users.Take(3))
        {
            Console.WriteLine($"   👤 Preparing user {user.Username} (ID: {user.Id})");
        }

        try
        {
            await context.Set<User>().AddRangeAsync(users);
            var saveResult = await context.SaveChangesAsync();

            activity?.SetTag("save_result", saveResult);
            metricsService.IncrementCounter($"seeding.users_seeded_successfully -> {users.Count}");
            activity?.SetTag("users_seeded_count", users.Count);

            var countAfterSave = await context.Set<User>().CountAsync();
            Console.WriteLine($"🔢 Users in database after SaveChanges: {countAfterSave}");

            if (countAfterSave != users.Count)
            {
                Console.WriteLine($"⚠️ WARNING: Expected {users.Count} users but found {countAfterSave} in database!");
                activity?.SetTag("users_count_mismatch", true);
                metricsService.IncrementCounter("seeding.users_count_mismatch");
            }

            var seedingDuration = DateTime.UtcNow - seedingStartTime;
            metricsService.RecordHistogram("seeding.user_seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetTag("seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving users: {ex.Message}");
            Console.WriteLine($"🔍 Inner exception: {ex.InnerException?.Message}");

            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.user_seeding_errors");

            throw;
        }
    }

    private static async Task SeedUserAddressesAsync(
        AddressDbContext addressContext, UserDbContext userContext,
        IMetricsService metricsService, Activity? parentActivity)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUserAddresses", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_addresses");

        Console.WriteLine("🌱 Starting address seeding...");

        var seedingStartTime = DateTime.UtcNow;
        var users = await userContext.Set<User>().ToListAsync();

        activity?.SetTag("users_found_for_address_seeding", users.Count);
        Console.WriteLine($"👥 Found {users.Count} users for address seeding");

        if (users.Count == 0)
        {
            Console.WriteLine("❌ No users found for address seeding, skipping.");
            metricsService.IncrementCounter("seeding.user_addresses_skipped_no_users");
            activity?.SetTag("skipped_no_users", true);
            activity?.SetStatus(ActivityStatusCode.Ok);
            return;
        }

        var addresses = new List<UserAddress>
        {
            new(users[0].Id, users[0], "123 Main St", "Apt 1", "New York", "NY", "10001") { Id = Guid.NewGuid() },
            new(users[1].Id, users[1], "456 Oak Ave", null, "Boston", "MA", "02108") { Id = Guid.NewGuid() },
            new(users[2].Id, users[2], "789 Pine Rd", "Suite 300", "Chicago", "IL", "60601") { Id = Guid.NewGuid() }
        };

        try
        {
            await addressContext.UserAddress.AddRangeAsync(addresses);
            var savedAddresses = await addressContext.SaveChangesAsync();

            metricsService.IncrementCounter($"seeding.user_addresses_seeded_successfully -> {addresses.Count}");
            activity?.SetTag("addresses_seeded_count", addresses.Count);

            var seedingDuration = DateTime.UtcNow - seedingStartTime;
            metricsService.RecordHistogram("seeding.address_seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetTag("seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            Console.WriteLine($"✅ Seeded {savedAddresses} addresses for {users.Count} users");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error seeding addresses: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.address_seeding_errors");
            throw;
        }
    }
}
