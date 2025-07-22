using System.Diagnostics;
using Bogus;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
using TBD.MetricsModule.Services.Interfaces;
using TBD.Shared.Repositories;
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
    public static async Task<List<User>> ReseedForTestingAsync(IServiceProvider serviceProvider,
        int numberOfFakeUsers = 4000)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.ReseedForTesting");
        activity?.SetTag("operation", "reseed_for_testing");
        activity?.SetTag("number_of_fake_users", numberOfFakeUsers);

        using var scope = serviceProvider.CreateScope();

        // Resolve DbContexts for schema management and repositories for data operations
        var userContext = scope.ServiceProvider.GetRequiredService<UserDbContext>();
        var addressContext = scope.ServiceProvider.GetRequiredService<AddressDbContext>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<User>>();
        var addressRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<UserAddress>>();

        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("UserModule");

        try
        {
            Console.WriteLine("🔄 Starting user database operations...");
            var operationStartTime = DateTime.UtcNow;

            await DeleteDatabasesAsync(userContext, addressContext, metricsService, activity);
            await MigrateDatabasesAsync(userContext, addressContext, metricsService, activity);

            // Seed users using the repository
            var seededUsers = await SeedUsersAsync(userRepository, metricsService, activity, numberOfFakeUsers);
            Console.WriteLine($"✅ Successfully seeded {seededUsers.Count} users");
            activity?.SetTag("users_seeded_count", seededUsers.Count);

            // Seed addresses using repositories
            await SeedUserAddressesAsync(userRepository, addressRepository, metricsService, activity);

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
            catch (SqlException ex) when (ex.Number == 2714)
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
        IGenericRepository<User> userRepository, IMetricsService metricsService, Activity? parentActivity, int count)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUsers", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_users");

        Console.WriteLine($"🌱 Starting user seeding for {count} users...");
        var seedingStartTime = DateTime.UtcNow;

        var hasher = new Hasher();
        var userFaker = new Faker<User>()
            .RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(u => u.Username, f => f.Internet.UserName())
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.Password,
                f => hasher.HashPassword(f.Internet.Password(32, memorable: true, prefix: "#Aa1")))
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(2))
            .RuleFor(u => u.UpdatedAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow));

        var users = new List<User>();
        var attempts = 0;
        var maxAttempts = count * 3;

        while (users.Count < count && attempts < maxAttempts)
        {
            var batchSize = Math.Min(1000, count - users.Count);
            var batch = userFaker.Generate(batchSize);
            var uniqueBatch = batch
                .GroupBy(u => u.Username, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                .GroupBy(u => u.Email, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                .Where(u => !users.Any(existing =>
                    existing is { Username: not null, Email: not null } &&
                    (existing.Username.Equals(u.Username, StringComparison.OrdinalIgnoreCase) ||
                     existing.Email.Equals(u.Email, StringComparison.OrdinalIgnoreCase))))
                .ToList();
            users.AddRange(uniqueBatch);
            attempts += batchSize;
        }

        users = users.Take(count).ToList();
        activity?.SetTag("users_to_create", users.Count);

        try
        {
            // Use the repository's efficient bulk insert method
            await userRepository.BulkInsertAsync(users);

            metricsService.IncrementCounter($"seeding.users_seeded_successfully -> {users.Count}");
            activity?.SetTag("users_seeded_count", users.Count);

            var seedingDuration = DateTime.UtcNow - seedingStartTime;
            metricsService.RecordHistogram("seeding.user_seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetTag("seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetStatus(ActivityStatusCode.Ok);

            Console.WriteLine($"✅ Bulk inserted {users.Count} users in {seedingDuration.TotalSeconds:F2} seconds.");
            return users;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving users with BulkInsert: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.user_seeding_errors");
            throw;
        }
    }

    private static async Task SeedUserAddressesAsync(
        IGenericRepository<User> userRepository, IGenericRepository<UserAddress> addressRepository,
        IMetricsService metricsService, Activity? parentActivity, int maxAddressesPerUser = 2)
    {
        using var activity = ActivitySource.StartActivity("DataSeeder.SeedUserAddressesStreaming",
            ActivityKind.Internal, parentActivity?.Context ?? default);

        Console.WriteLine("🌱 Starting streaming address seeding...");
        var seedingStartTime = DateTime.UtcNow;
        var totalAddresses = 0;
        var processedUsers = 0;
        var addressBatch = new List<UserAddress>();
        const int batchSize = 5000;

        try
        {
            // Stream users from the repository to keep memory usage low
            await foreach (var user in userRepository.GetAllStreamingAsync(bufferSize: 1000))
            {
                var faker = new Faker();
                var numberOfAddressesForUser = faker.Random.Int(1, maxAddressesPerUser);

                var userAddressFaker = new Faker<UserAddress>()
                    .RuleFor(ua => ua.Id, _ => Guid.NewGuid())
                    .RuleFor(ua => ua.UserId, _ => user.Id)
                    .RuleFor(ua => ua.Address1, f => f.Address.StreetAddress())
                    .RuleFor(ua => ua.Address2, f => f.Address.SecondaryAddress())
                    .RuleFor(ua => ua.City, f => f.Address.City())
                    .RuleFor(ua => ua.State, f => f.Address.StateAbbr())
                    .RuleFor(ua => ua.ZipCode, f => f.Address.ZipCode());

                addressBatch.AddRange(userAddressFaker.Generate(numberOfAddressesForUser));
                processedUsers++;

                if (addressBatch.Count < batchSize)
                {
                    continue;
                }

                await addressRepository.BulkInsertAsync(addressBatch);
                totalAddresses += addressBatch.Count;
                addressBatch.Clear();
                Console.WriteLine(
                    $"📈 Processed {processedUsers:N0} users, bulk inserted {totalAddresses:N0} addresses");
            }

            if (addressBatch.Count > 0)
            {
                await addressRepository.BulkInsertAsync(addressBatch);
                totalAddresses += addressBatch.Count;
            }

            var seedingDuration = DateTime.UtcNow - seedingStartTime;
            metricsService.IncrementCounter($"seeding.user_addresses_seeded_successfully -> {totalAddresses}");
            metricsService.RecordHistogram("seeding.address_seeding_duration_seconds", seedingDuration.TotalSeconds);
            activity?.SetTag("addresses_seeded_count", totalAddresses);
            activity?.SetStatus(ActivityStatusCode.Ok);

            Console.WriteLine(
                $"✅ Streaming seeded {totalAddresses:N0} addresses for {processedUsers:N0} users in {seedingDuration.TotalSeconds:F2} seconds");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during streaming address seeding: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.address_seeding_errors");
            throw;
        }
    }
}
