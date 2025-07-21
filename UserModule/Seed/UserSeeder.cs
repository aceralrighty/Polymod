using System.Diagnostics;
using Bogus;
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

public static class UserSeeder
{
    private static readonly ActivitySource ActivitySource = new("TBD.UserModule.DataSeeder");

    /// <summary>
    /// Reseeds the database for testing purposes, generating a specified number of fake users and their addresses.
    /// </summary>
    /// <param name="serviceProvider">The service provider to resolve dependencies.</param>
    /// <param name="numberOfFakeUsers">The number of fake users to generate. Defaults to 50 if not specified.</param>
    /// <returns>A list of the seeded users.</returns>
    public static async Task<List<User>> ReseedForTestingAsync(IServiceProvider serviceProvider, int numberOfFakeUsers = 50)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.ReseedForTesting");
        activity?.SetTag("operation", "reseed_for_testing");
        activity?.SetTag("number_of_fake_users", numberOfFakeUsers);

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

            // Pass numberOfFakeUsers to SeedUsersAsync
            var seededUsers = await SeedUsersAsync(userContext, metricsService, activity, numberOfFakeUsers);
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
            Console.WriteLine("📝 Users in database (first 3):");
            foreach (var user in usersInDb)
            {
                Console.WriteLine($"   👤 {user.Username} (ID: {user.Id})");
            }

            await SeedUserAddressesAsync(addressContext, userContext, metricsService, activity, numberOfFakeUsers); // Pass numberOfFakeUsers
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
        UserDbContext context, IMetricsService metricsService, Activity? parentActivity, int count)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUsers", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_users");

        Console.WriteLine($"🌱 Starting user seeding for {count} users...");

        var seedingStartTime = DateTime.UtcNow;
        var hasher = new Hasher(); // Assuming Hasher is a utility for password hashing

        // Configure a Faker for the User model
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, f => Guid.NewGuid())
            .RuleFor(u => u.Username, f => f.Internet.UserName(f.Name.FirstName(), f.Name.LastName()))
            .RuleFor(u => u.Email, (f, u) => f.Internet.Email(u.Username))
            .RuleFor(u => u.Password,
                f => hasher.HashPassword(f.Internet.Password(8, memorable: true, prefix: "#Aa1"))) // Strong password
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2)) // Created up to 2 years ago
            .RuleFor(u => u.UpdatedAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow));

        // Generate the specified number of fake users
        var users = userFaker.Generate(count);

        activity?.SetTag("users_to_create", users.Count);

        foreach (var user in users.Take(Math.Min(3, users.Count))) // Log only the first few for brevity
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
        IMetricsService metricsService, Activity? parentActivity, int maxAddressesPerUser = 2)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUserAddresses", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_addresses");

        Console.WriteLine("🌱 Starting address seeding...");

        var seedingStartTime = DateTime.UtcNow;
        // Fetch all users to associate addresses
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

        var addresses = new List<UserAddress>();
        var faker = new Faker(); // General faker instance

        foreach (var user in users)
        {
            // For each user, generate 1 to maxAddressesPerUser addresses
            var numberOfAddressesForUser = faker.Random.Int(1, maxAddressesPerUser);
            var userAddressFaker = new Faker<UserAddress>()
                .RuleFor(ua => ua.Id, f => Guid.NewGuid())
                .RuleFor(ua => ua.UserId, f => user.Id)
                .RuleFor(ua => ua.User, f => user) // Link to the user object (if needed for EF Core)
                .RuleFor(ua => ua.Address1, f => f.Address.StreetAddress())
                .RuleFor(ua => ua.Address2, f => f.Address.SecondaryAddress())
                .RuleFor(ua => ua.City, f => f.Address.City())
                .RuleFor(ua => ua.State, f => f.Address.StateAbbr())
                .RuleFor(ua => ua.ZipCode, f => f.Address.ZipCode());

            addresses.AddRange(userAddressFaker.Generate(numberOfAddressesForUser));
        }

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
