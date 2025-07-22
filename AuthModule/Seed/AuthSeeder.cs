using System.Diagnostics;
using Bogus;
using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.MetricsModule.Services.Interfaces;
using TBD.Shared.Utils;

namespace TBD.AuthModule.Seed;

public static class AuthSeeder
{
    private static readonly ActivitySource ActivitySource = new("TBD.AuthModule.AuthSeeder");

    public static async Task ReseedAsync(IServiceProvider serviceProvider)
    {
        using var activity = ActivitySource.StartActivity("AuthSeeder.ReseedSeed");
        activity?.SetTag("operation", "reseed_seed");

        using var scope = serviceProvider.CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("AuthModule");

        try
        {
            metricsService.IncrementCounter("seeding.database_recreate_started");
            Console.WriteLine("🗑️ Deleting Auth database...");
            await authContext.Database.EnsureDeletedAsync();

            Console.WriteLine("📊 Creating Auth database...");
            await authContext.Database.EnsureCreatedAsync();

            await authContext.SaveChangesAsync();
            metricsService.IncrementCounter("seeding.database_recreate_completed");

            await SeedAuthAsync(authContext, metricsService, activity);

            metricsService.IncrementCounter("seeding.full_reseed_completed");
            activity?.SetStatus(ActivityStatusCode.Ok);
            Console.WriteLine("✅ Auth reseed completed successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error during Auth reseed: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("error.type", ex.GetType().Name);
            activity?.SetTag("error.message", ex.Message);

            metricsService.IncrementCounter("seeding.errors_total");
            metricsService.IncrementCounter($"seeding.errors_{ex.GetType().Name}");

            throw;
        }
    }

    private static async Task SeedAuthAsync(AuthDbContext authContext, IMetricsService metricsService,
        Activity? parentActivity)
    {
        using var activity = ActivitySource.StartActivity("AuthSeeder.SeedAuth", ActivityKind.Internal,
            parentActivity?.Context ?? default);
        activity?.SetTag("step", "seed_auth");

        Console.WriteLine("🌱 Starting Auth user seeding...");
        metricsService.IncrementCounter("seeding.auth_seed_started");

        var hasher = new Hasher();
        var now = DateTime.UtcNow;
        var authFaker = new Faker<AuthUser>().RuleFor(u => u.Id, _ => Guid.NewGuid())
            .RuleFor(a => a.Email, f => f.Person.Email).RuleFor(a => a.Username, f => f.Person.UserName)
            .RuleFor(a => a.HashedPassword, f => hasher.HashPassword(f.Internet.Password(16, memorable: true)))
            .RuleFor(a => a.RefreshToken, _ => JwtTokenGenerator.GenerateJwtToken(64))
            .RuleFor(a => a.RefreshTokenExpiry, f => f.Date.Soon(20))
            .RuleFor(a => a.FailedLoginAttempts, f => f.Random.Int(0, 4))
            .RuleFor(a => a.CreatedAt, f => f.Date.Past(6))
            .RuleFor(a => a.UpdatedAt, f => f.Date.Future(7))
            .RuleFor(a => a.LastLogin, f => f.Date.Past(5));


        var authUsers = new List<AuthUser>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Email = "sarah.johnson@techcorp.com",
                HashedPassword = hasher.HashPassword("SecurePass123!"),
                Username = "sarahj_dev",
                FailedLoginAttempts = 0,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
                RefreshTokenExpiry = now.AddDays(30),
                CreatedAt = now.AddDays(-45),
                UpdatedAt = now.AddDays(-2),
                LastLogin = now.AddHours(-6)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "michael.chen@innovatesoft.com",
                HashedPassword = hasher.HashPassword("CloudNinja2024"),
                Username = "mchen_architect",
                FailedLoginAttempts = 1,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(22),
                CreatedAt = now.AddDays(-30),
                UpdatedAt = now.AddDays(-5),
                LastLogin = now.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "emily.rodriguez@cyberdyne.net",
                HashedPassword = hasher.HashPassword("Terminator$5"),
                Username = "emily_cyber",
                FailedLoginAttempts = 4,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
                RefreshTokenExpiry = now.AddDays(6),
                CreatedAt = now.AddDays(-90),
                UpdatedAt = now.AddDays(-7),
                LastLogin = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "david.kim@fusiontech.io",
                HashedPassword = hasher.HashPassword("Fusion123!"),
                Username = "david_devops",
                FailedLoginAttempts = 0,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(45),
                CreatedAt = now.AddDays(-120),
                UpdatedAt = now.AddDays(-30),
                LastLogin = now.AddDays(-20)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "jessica.lee@nextgenapps.com",
                HashedPassword = hasher.HashPassword("NextGen2025"),
                Username = "jessica_uiux",
                FailedLoginAttempts = 2,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(5),
                CreatedAt = now.AddDays(-15),
                UpdatedAt = now.AddDays(-1),
                LastLogin = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "thomas.brown@secureware.com",
                HashedPassword = hasher.HashPassword("Admin!Secure"),
                Username = "thomas_admin",
                FailedLoginAttempts = 6,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(2),
                CreatedAt = now.AddDays(-180),
                UpdatedAt = now.AddDays(-5),
                LastLogin = now.AddDays(-10),
                DeletedAt = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "rachel.nguyen@datapulse.io",
                HashedPassword = hasher.HashPassword("DataWrangler2023"),
                Username = "rachel_data",
                FailedLoginAttempts = 0,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
                RefreshTokenExpiry = now.AddDays(35),
                CreatedAt = now.AddDays(-60),
                UpdatedAt = now.AddDays(-2),
                LastLogin = now.AddDays(-3)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "kevin.morris@blockchainlabs.com",
                HashedPassword = hasher.HashPassword("ChainReact!99"),
                Username = "kevin_bc",
                FailedLoginAttempts = 3,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(4),
                CreatedAt = now.AddDays(-100),
                UpdatedAt = now.AddDays(-15),
                LastLogin = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "linda.garcia@meditech.ai",
                HashedPassword = hasher.HashPassword("MediTechAI"),
                Username = "linda_mlops",
                FailedLoginAttempts = 1,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
                RefreshTokenExpiry = now.AddDays(28),
                CreatedAt = now.AddDays(-20),
                UpdatedAt = now.AddDays(-2),
                LastLogin = now.AddDays(-1)
            },
            new()
            {
                Id = Guid.NewGuid(),
                Email = "brian.thompson@retrotech.org",
                HashedPassword = hasher.HashPassword("RetroLove1990"),
                Username = "brian_retro",
                FailedLoginAttempts = 8,
                RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
                RefreshTokenExpiry = now.AddDays(1),
                CreatedAt = now.AddDays(-365),
                UpdatedAt = now.AddDays(-60),
                LastLogin = now.AddDays(-90),
                DeletedAt = now.AddDays(-10)
            },
        };

        try
        {
            var testAuthUsers = authFaker.Generate(50);
            var allUsers = authUsers.Concat(testAuthUsers).ToList();
            await authContext.AuthUsers.AddRangeAsync(allUsers);
            var savedCount = await authContext.SaveChangesAsync();
            Console.WriteLine($"✅ Seeded {savedCount} Auth users.");

            var totalUsers = authUsers.Count;
            metricsService.IncrementCounter($"seeding.users_created_total -> {totalUsers}");

            var deletedUsers = allUsers.Count(u => u.DeletedAt.HasValue);
            var activeUsers = totalUsers - deletedUsers;
            var secureUsers = allUsers.Count(IsSecureUser);
            var riskUsers = allUsers.Count(IsRiskUser);
            var recentUsers = allUsers.Count(IsRecentUser);
            var longTokenUsers = allUsers.Count(IsLongTokenUser);
            var shortTokenUsers = allUsers.Count(IsShortTokenUser);
            var frequentFailedLoginUsers = allUsers.Count(u => u.FailedLoginAttempts >= 3);
            var expiringSoonUsers = allUsers.Count(IsExpiringSoon);

            metricsService.IncrementCounter($"seeding.users_created_active -> {activeUsers}");
            metricsService.IncrementCounter($"seeding.users_created_deleted -> {deletedUsers}");
            metricsService.IncrementCounter($"seeding.users_created_secure -> {secureUsers}");
            metricsService.IncrementCounter($"seeding.users_created_risk -> {riskUsers}");
            metricsService.IncrementCounter($"seeding.users_created_recent -> {recentUsers}");
            metricsService.IncrementCounter($"seeding.users_created_long_token -> {longTokenUsers}");
            metricsService.IncrementCounter($"seeding.users_created_short_token -> {shortTokenUsers}");
            metricsService.IncrementCounter(
                $"seeding.users_created_frequent_failed_logins -> {frequentFailedLoginUsers}");
            metricsService.IncrementCounter($"seeding.users_created_expiring_soon -> {expiringSoonUsers}");

            metricsService.IncrementCounter("seeding.database_save_completed");
            metricsService.IncrementCounter("seeding.auth_seed_completed");

            activity?.SetTag("users_seeded_total", totalUsers);
            activity?.SetStatus(ActivityStatusCode.Ok);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error saving Auth users: {ex.Message}");
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            metricsService.IncrementCounter("seeding.auth_seed_errors");
            throw;
        }
    }

    private static bool IsSecureUser(AuthUser user)
    {
        return user.FailedLoginAttempts == 0 &&
               user.RefreshTokenExpiry > DateTime.UtcNow.AddDays(20) &&
               !user.DeletedAt.HasValue;
    }

    private static bool IsRiskUser(AuthUser user)
    {
        return user.FailedLoginAttempts >= 5 ||
               user.RefreshTokenExpiry < DateTime.UtcNow.AddDays(7) ||
               user.DeletedAt.HasValue;
    }

    private static bool IsRecentUser(AuthUser user)
    {
        return user.LastLogin > DateTime.UtcNow.AddDays(-7);
    }

    private static bool IsLongTokenUser(AuthUser user)
    {
        return user.RefreshTokenExpiry > DateTime.UtcNow.AddDays(25);
    }

    private static bool IsShortTokenUser(AuthUser user)
    {
        return user.RefreshTokenExpiry < DateTime.UtcNow.AddDays(10);
    }

    private static bool IsExpiringSoon(AuthUser user)
    {
        return user.RefreshTokenExpiry < DateTime.UtcNow.AddDays(7);
    }
}
