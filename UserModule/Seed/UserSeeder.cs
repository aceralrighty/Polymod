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
            .RuleFor(u => u.CreatedAt, f => f.Date.Past(10))
            .RuleFor(u => u.UpdatedAt, (f, u) => f.Date.Between(u.CreatedAt, DateTime.UtcNow))
            .RuleFor(u => u.DeletedAt, (f, u) => f.Date.Past(10) > u.CreatedAt ? f.Date.Past(10) : null);

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

        // Performance tracking setup
        var seedingStartTime = DateTime.UtcNow;
        var memoryBefore = GC.GetTotalMemory(false);
        var gen0CollectionsBefore = GC.CollectionCount(0);
        var gen1CollectionsBefore = GC.CollectionCount(1);
        var gen2CollectionsBefore = GC.CollectionCount(2);

        var totalAddresses = 0;
        var processedUsers = 0;
        var addressBatch = new List<UserAddress>();
        const int batchSize = 1000;

        var batchTimes = new List<double>();
        var throughputMeasurements = new List<double>();

        // Create Faker instances once, outside the loop to reduce memory usage
        var faker = new Faker();
        var userAddressFaker = new Faker<UserAddress>()
            .RuleFor(ua => ua.Id, _ => Guid.NewGuid())
            .RuleFor(ua => ua.UserId, _ => Guid.Empty) // Will be set per user after generation
            .RuleFor(ua => ua.Address1, f => f.Address.StreetAddress())
            .RuleFor(ua => ua.Address2, f => f.Random.Bool(0.3f) ? f.Address.SecondaryAddress() : null)
            .RuleFor(ua => ua.City, f => f.Address.City())
            .RuleFor(ua => ua.State, f => f.Address.StateAbbr())
            .RuleFor(ua => ua.ZipCode, f => f.Address.ZipCode())
            .RuleFor(ua => ua.CreatedAt, f => f.Date.Past(10))
            .RuleFor(ua => ua.UpdatedAt, (f, ua) => f.Date.Between(ua.CreatedAt, DateTime.UtcNow))
            .RuleFor(ua => ua.DeletedAt, (f, ua) => f.Date.Past(10) > ua.CreatedAt ? f.Date.Past(10) : null);

        try
        {
            var batchStartTime = DateTime.UtcNow;
            var batchNumber = 0;

            // Stream users from the repository to keep memory usage low
            await foreach (var user in userRepository.GetAllStreamingAsync(bufferSize: 1000))
            {
                var numberOfAddressesForUser = faker.Random.Int(1, maxAddressesPerUser);

                // Generate addresses using the reusable faker instance
                var userAddresses = userAddressFaker.Generate(numberOfAddressesForUser);

                // Set the UserId for each generated address
                foreach (var address in userAddresses)
                {
                    address.UserId = user.Id;
                }

                addressBatch.AddRange(userAddresses);
                processedUsers++;

                if (addressBatch.Count < batchSize)
                {
                    continue;
                }

                batchNumber++;

                // Track batch performance with OpenTelemetry
                var batchInsertStart = DateTime.UtcNow;
                await addressRepository.BulkInsertAsync(addressBatch);
                var batchInsertDuration = (DateTime.UtcNow - batchInsertStart).TotalMilliseconds;
                batchTimes.Add(batchInsertDuration);

                totalAddresses += addressBatch.Count;

                // Calculate throughput for this batch
                var batchProcessingTime = (DateTime.UtcNow - batchStartTime).TotalSeconds;
                var addressesPerSecond = addressBatch.Count / batchProcessingTime;
                throughputMeasurements.Add(addressesPerSecond);

                // Record comprehensive metrics with tags using your OpenTelemetry service
                var batchTags = new KeyValuePair<string, object?>[]
                {
                    new("batch_number", batchNumber), new("batch_size", addressBatch.Count),
                    new("operation", "bulk_insert"), new("optimization", "shared_faker")
                };

                metricsService.RecordHistogram("seeding.batch_insert_duration_ms", batchInsertDuration, batchTags);
                metricsService.RecordHistogram("seeding.addresses_per_second", addressesPerSecond, batchTags);
                metricsService.RecordHistogram("seeding.batch_processing_time_ms", batchProcessingTime * 1000,
                    batchTags);
                metricsService.RecordHistogram("seeding.memory_usage_mb", GC.GetTotalMemory(false) / (1024.0 * 1024.0),
                    batchTags);

                // Track cumulative progress
                var progressTags = new KeyValuePair<string, object?>[]
                {
                    new("total_addresses", totalAddresses), new("total_users", processedUsers)
                };
                metricsService.RecordHistogram("seeding.cumulative_addresses", totalAddresses, progressTags);
                metricsService.RecordHistogram("seeding.cumulative_users", processedUsers, progressTags);

                addressBatch.Clear();
                batchStartTime = DateTime.UtcNow;

                Console.WriteLine(
                    $"📈 Batch {batchNumber}: Processed {processedUsers:N0} users, inserted {totalAddresses:N0} addresses " +
                    $"(Duration: {batchInsertDuration:F1}ms, Throughput: {addressesPerSecond:F0} addr/sec)");
            }

            // Handle remaining addresses in the final batch
            if (addressBatch.Count > 0)
            {
                batchNumber++;
                var finalBatchStart = DateTime.UtcNow;
                await addressRepository.BulkInsertAsync(addressBatch);
                var finalBatchDuration = (DateTime.UtcNow - finalBatchStart).TotalMilliseconds;
                batchTimes.Add(finalBatchDuration);
                totalAddresses += addressBatch.Count;

                var finalBatchTags = new KeyValuePair<string, object?>[]
                {
                    new("batch_number", batchNumber), new("batch_size", addressBatch.Count),
                    new("operation", "bulk_insert_final"), new("optimization", "shared_faker")
                };

                metricsService.RecordHistogram("seeding.batch_insert_duration_ms", finalBatchDuration, finalBatchTags);
            }

            // Calculate comprehensive final performance metrics
            var seedingDuration = DateTime.UtcNow - seedingStartTime;
            var memoryAfter = GC.GetTotalMemory(false);
            var memoryUsedMb = (memoryAfter - memoryBefore) / (1024.0 * 1024.0);
            var gen0Collections = GC.CollectionCount(0) - gen0CollectionsBefore;
            var gen1Collections = GC.CollectionCount(1) - gen1CollectionsBefore;
            var gen2Collections = GC.CollectionCount(2) - gen2CollectionsBefore;

            var overallThroughput = totalAddresses / seedingDuration.TotalSeconds;
            var avgBatchTime = batchTimes.Count > 0 ? batchTimes.Average() : 0;
            var p95BatchTime = batchTimes.Count > 0
                ? batchTimes.OrderBy(x => x).Skip((int)(batchTimes.Count * 0.95)).FirstOrDefault()
                : 0;
            var maxThroughput = throughputMeasurements.Count > 0 ? throughputMeasurements.Max() : 0;
            var avgThroughput = throughputMeasurements.Count > 0 ? throughputMeasurements.Average() : 0;
            var gcPressureScore = gen0Collections + gen1Collections * 2 + gen2Collections * 4;
            var efficiencyScore = totalAddresses / Math.Max(1, gen2Collections);

            // Record final summary metrics with comprehensive tags
            var summaryTags = new KeyValuePair<string, object?>[]
            {
                new("total_addresses", totalAddresses), new("total_users", processedUsers),
                new("total_batches", batchNumber), new("optimization_type", "shared_faker_instance"),
                new("batch_size", batchSize), new("max_addresses_per_user", maxAddressesPerUser)
            };

            // Core performance metrics
            metricsService.RecordHistogram("seeding.total_duration_seconds", seedingDuration.TotalSeconds, summaryTags);
            metricsService.RecordHistogram("seeding.overall_throughput_addresses_per_second", overallThroughput,
                summaryTags);
            metricsService.RecordHistogram("seeding.peak_throughput_addresses_per_second", maxThroughput, summaryTags);
            metricsService.RecordHistogram("seeding.average_throughput_addresses_per_second", avgThroughput,
                summaryTags);

            // Memory and efficiency metrics
            metricsService.RecordHistogram("seeding.total_memory_used_mb", memoryUsedMb, summaryTags);
            metricsService.RecordHistogram("seeding.memory_per_address_bytes",
                (memoryAfter - memoryBefore) / (double)totalAddresses, summaryTags);

            // Latency metrics
            metricsService.RecordHistogram("seeding.average_batch_time_ms", avgBatchTime, summaryTags);
            metricsService.RecordHistogram("seeding.p95_batch_time_ms", p95BatchTime, summaryTags);

            // Garbage collection metrics
            var gcTags = new KeyValuePair<string, object?>[]
            {
                new("gc_generation", "gen0"), new("optimization_type", "shared_faker_instance")
            };
            metricsService.RecordHistogram("seeding.gc_collections", gen0Collections, gcTags);

            gcTags[0] = new("gc_generation", "gen1");
            metricsService.RecordHistogram("seeding.gc_collections", gen1Collections, gcTags);

            gcTags[0] = new("gc_generation", "gen2");
            metricsService.RecordHistogram("seeding.gc_collections", gen2Collections, gcTags);

            metricsService.RecordHistogram("seeding.gc_pressure_score", gcPressureScore, summaryTags);
            metricsService.RecordHistogram("seeding.addresses_per_gen2_gc", efficiencyScore, summaryTags);

            // Success counter
            metricsService.IncrementCounter("seeding.user_addresses_completed_successfully");

            // Set activity tags for distributed tracing
            activity?.SetTag("addresses_seeded_count", totalAddresses);
            activity?.SetTag("users_processed_count", processedUsers);
            activity?.SetTag("overall_throughput_per_sec", overallThroughput.ToString("F0"));
            activity?.SetTag("memory_used_mb", memoryUsedMb.ToString("F1"));
            activity?.SetTag("avg_batch_time_ms", avgBatchTime.ToString("F1"));
            activity?.SetTag("gc_pressure_score", gcPressureScore.ToString());
            activity?.SetTag("optimization_type", "shared_faker_instance");
            activity?.SetStatus(ActivityStatusCode.Ok);

            // Comprehensive performance summary for console
            Console.WriteLine("🚀 === SEEDING PERFORMANCE SUMMARY ===");
            Console.WriteLine(
                $"✅ Seeded {totalAddresses:N0} addresses for {processedUsers:N0} users in {batchNumber} batches");
            Console.WriteLine($"⏱️  Total Duration: {seedingDuration.TotalSeconds:F2} seconds");
            Console.WriteLine($"🔥 Overall Throughput: {overallThroughput:F0} addresses/second");
            Console.WriteLine($"📊 Peak Throughput: {maxThroughput:F0} addresses/second");
            Console.WriteLine($"📊 Average Throughput: {avgThroughput:F0} addresses/second");
            Console.WriteLine(
                $"💾 Memory Usage: {memoryUsedMb:F1} MB ({memoryUsedMb * 1024 / totalAddresses:F2} KB per address)");
            Console.WriteLine($"⚡ Average Batch Time: {avgBatchTime:F1} ms");
            Console.WriteLine($"⚡ P95 Batch Time: {p95BatchTime:F1} ms");
            Console.WriteLine(
                $"🗑️  GC Pressure: Gen0={gen0Collections}, Gen1={gen1Collections}, Gen2={gen2Collections} (Score: {gcPressureScore})");
            Console.WriteLine($"💡 Efficiency: {efficiencyScore:F0} addresses per Gen2 GC");
            Console.WriteLine($"🎯 Optimization: Shared Faker Instance (Memory Optimized)");
            Console.WriteLine("==========================================");
        }
        catch (Exception ex)
        {
            // Record error metrics
            var errorTags = new KeyValuePair<string, object?>[]
            {
                new("error_type", ex.GetType().Name), new("addresses_processed_before_error", totalAddresses),
                new("users_processed_before_error", processedUsers)
            };

            metricsService.RecordHistogram("seeding.error_occurred", 1, errorTags);
            metricsService.IncrementCounter("seeding.address_seeding_errors");

            Console.WriteLine($"❌ Error during streaming address seeding: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            throw;
        }
    }
}
