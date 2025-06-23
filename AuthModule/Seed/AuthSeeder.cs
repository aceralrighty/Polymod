using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.MetricsModule.Services;
using TBD.MetricsModule.Services.Interfaces;
using TBD.Shared.Utils;

namespace TBD.AuthModule.Seed;

public static class AuthSeeder
{
    public static async Task ReseedSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        var factory = scope.ServiceProvider.GetRequiredService<IMetricsServiceFactory>();
        var metricsService = factory.CreateMetricsService("AuthModule");


        metricsService.IncrementCounter("seeding.database_recreate_started");

        await authContext.Database.EnsureDeletedAsync();
        await authContext.Database.EnsureCreatedAsync();
        await authContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.database_recreate_completed");

        await SeedAuthAsync(authContext, metricsService);

        metricsService.IncrementCounter("seeding.full_reseed_completed");
    }

    private static async Task SeedAuthAsync(AuthDbContext authContext, IMetricsService metricsService)
    {
        metricsService.IncrementCounter("seeding.auth_seed_started");

        var auths = new List<AuthUser>();
        var hasher = new Hasher();

        var auth1 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "sarah.johnson@techcorp.com",
            HashedPassword = hasher.HashPassword("SecurePass123!"),
            Username = "sarahj_dev",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow.AddDays(-45),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            LastLogin = DateTime.UtcNow.AddHours(-6)
        };

        var auth2 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "michael.chen@innovatesoft.com",
            HashedPassword = hasher.HashPassword("CloudNinja2024"),
            Username = "mchen_architect",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(22),
            CreatedAt = DateTime.UtcNow.AddDays(-30),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            LastLogin = DateTime.UtcNow.AddDays(-3)
        };

        var auth3 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "emily.rodriguez@startupvibe.io",
            HashedPassword = hasher.HashPassword("InnovateDaily"),
            Username = "emily_pm",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(48),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(25),
            CreatedAt = DateTime.UtcNow.AddDays(-60),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            LastLogin = DateTime.UtcNow.AddHours(-12)
        };

        var auth4 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "david.kumar@datascience.net",
            HashedPassword = hasher.HashPassword("Analytics2024!"),
            Username = "dkumar_analyst",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(18),
            CreatedAt = DateTime.UtcNow.AddDays(-25),
            UpdatedAt = DateTime.UtcNow.AddDays(-4),
            LastLogin = DateTime.UtcNow.AddDays(-2)
        };

        var auth5 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "lisa.thompson@cloudservices.com",
            HashedPassword = hasher.HashPassword("DevOpsRocks"),
            Username = "lisa_devops",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(72),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(28),
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            LastLogin = DateTime.UtcNow.AddHours(-18)
        };

        var auth6 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "robert.wilson@cybersec.org",
            HashedPassword = hasher.HashPassword("Security1st!"),
            Username = "rwilson_sec",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(35),
            CreatedAt = DateTime.UtcNow.AddDays(-50),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            LastLogin = DateTime.UtcNow.AddHours(-4)
        };

        var auth7 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "amanda.garcia@frontend.dev",
            HashedPassword = hasher.HashPassword("ReactMaster"),
            Username = "amanda_ui",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(12),
            CreatedAt = DateTime.UtcNow.AddDays(-35),
            UpdatedAt = DateTime.UtcNow.AddDays(-7),
            LastLogin = DateTime.UtcNow.AddDays(-5)
        };

        var auth8 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "james.brown@backend.tech",
            HashedPassword = hasher.HashPassword("ApiNinja2024"),
            Username = "james_api",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(40),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(20),
            CreatedAt = DateTime.UtcNow.AddDays(-28),
            UpdatedAt = DateTime.UtcNow.AddDays(-6),
            LastLogin = DateTime.UtcNow.AddDays(-1)
        };

        var auth9 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "jennifer.lee@mobile.apps",
            HashedPassword = hasher.HashPassword("MobileFirst!"),
            Username = "jenny_mobile",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(56),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(24),
            CreatedAt = DateTime.UtcNow.AddDays(-42),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            LastLogin = DateTime.UtcNow.AddHours(-8)
        };

        var auth10 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "daniel.martin@qa.testing",
            HashedPassword = hasher.HashPassword("QualityFirst"),
            Username = "dan_tester",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow.AddDays(-20),
            UpdatedAt = DateTime.UtcNow.AddDays(-8),
            LastLogin = DateTime.UtcNow.AddDays(-4),
            DeletedAt = DateTime.UtcNow.AddDays(-2)
        };

        var auth11 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "rachel.white@ux.design",
            HashedPassword = hasher.HashPassword("DesignThinking"),
            Username = "rachel_ux",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(68),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(26),
            CreatedAt = DateTime.UtcNow.AddDays(-38),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            LastLogin = DateTime.UtcNow.AddHours(-14)
        };

        var auth12 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "kevin.taylor@database.admin",
            HashedPassword = hasher.HashPassword("DataSafety2024"),
            Username = "kevin_dba",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(44),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(16),
            CreatedAt = DateTime.UtcNow.AddDays(-33),
            UpdatedAt = DateTime.UtcNow.AddDays(-4),
            LastLogin = DateTime.UtcNow.AddDays(-2)
        };

        var auth13 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "stephanie.adams@content.mgmt",
            HashedPassword = hasher.HashPassword("ContentKing"),
            Username = "steph_cms",
            FailedLoginAttempts = 4,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(8),
            CreatedAt = DateTime.UtcNow.AddDays(-55),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            LastLogin = DateTime.UtcNow.AddDays(-8)
        };

        var auth14 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "brian.clark@marketing.tech",
            HashedPassword = hasher.HashPassword("GrowthHack"),
            Username = "brian_growth",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(52),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(21),
            CreatedAt = DateTime.UtcNow.AddDays(-27),
            UpdatedAt = DateTime.UtcNow.AddDays(-5),
            LastLogin = DateTime.UtcNow.AddDays(-1)
        };

        var auth15 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "maria.gonzalez@hr.solutions",
            HashedPassword = hasher.HashPassword("PeopleFirst!"),
            Username = "maria_hr",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(60),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(29),
            CreatedAt = DateTime.UtcNow.AddDays(-48),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            LastLogin = DateTime.UtcNow.AddHours(-10)
        };

        var auth16 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "thomas.anderson@finance.corp",
            HashedPassword = hasher.HashPassword("NumbersCrunch"),
            Username = "tom_finance",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(11),
            CreatedAt = DateTime.UtcNow.AddDays(-22),
            UpdatedAt = DateTime.UtcNow.AddDays(-6),
            LastLogin = DateTime.UtcNow.AddDays(-3)
        };

        var auth17 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "nicole.walker@support.help",
            HashedPassword = hasher.HashPassword("HelpDesk24"),
            Username = "nicole_support",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(38),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(17),
            CreatedAt = DateTime.UtcNow.AddDays(-31),
            UpdatedAt = DateTime.UtcNow.AddDays(-4),
            LastLogin = DateTime.UtcNow.AddHours(-20)
        };

        var auth18 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "alex.peterson@legal.tech",
            HashedPassword = hasher.HashPassword("ComplianceKey"),
            Username = "alex_legal",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(46),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(9),
            CreatedAt = DateTime.UtcNow.AddDays(-26),
            UpdatedAt = DateTime.UtcNow.AddDays(-7),
            LastLogin = DateTime.UtcNow.AddDays(-4)
        };

        var auth19 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "samantha.hill@sales.team",
            HashedPassword = hasher.HashPassword("CloseDeals2024"),
            Username = "sam_sales",
            FailedLoginAttempts = 5,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(6),
            CreatedAt = DateTime.UtcNow.AddDays(-18),
            UpdatedAt = DateTime.UtcNow.AddDays(-9),
            LastLogin = DateTime.UtcNow.AddDays(-7)
        };

        var auth20 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "christopher.moore@executive.suite",
            HashedPassword = hasher.HashPassword("Leadership2024!"),
            Username = "chris_exec",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(80),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(45),
            CreatedAt = DateTime.UtcNow.AddDays(-90),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            LastLogin = DateTime.UtcNow.AddHours(-2)
        };

        // Add all users to the list
        auths.AddRange([
            auth1, auth2, auth3, auth4, auth5, auth6, auth7, auth8, auth9, auth10, auth11, auth12, auth13, auth14,
            auth15, auth16, auth17, auth18, auth19, auth20
        ]);

        // Track users by various categories
        var deletedUsers = auths.Count(u => u.DeletedAt.HasValue);
        var activeUsers = auths.Count - deletedUsers;
        var secureUsers = auths.Count(IsSecureUser);
        var riskUsers = auths.Count(IsRiskUser);
        var recentUsers = auths.Count(IsRecentUser);
        var longTokenUsers = auths.Count(IsLongTokenUser);
        var shortTokenUsers = auths.Count(IsShortTokenUser);
        var frequentFailedLoginUsers = auths.Count(u => u.FailedLoginAttempts >= 3);
        var expiringSoonUsers = auths.Count(IsExpiringSoon);

        // Log total users created
        for (var i = 0; i < auths.Count; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_total");
        }

        // Log user status categories
        for (var i = 0; i < activeUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_active");
        }

        for (var i = 0; i < deletedUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_deleted");
        }

        // Log-security-related categories
        for (var i = 0; i < secureUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_secure");
        }

        for (var i = 0; i < riskUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_risk");
        }

        for (var i = 0; i < frequentFailedLoginUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_frequent_failed_logins");
        }

        // Log activity-related categories
        for (var i = 0; i < recentUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_recent");
        }

        for (var i = 0; i < expiringSoonUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_expiring_soon");
        }

        // Log token-related categories
        for (var i = 0; i < longTokenUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_long_token");
        }

        for (var i = 0; i < shortTokenUsers; i++)
        {
            metricsService.IncrementCounter("seeding.users_created_short_token");
        }

        await authContext.AuthUsers.AddRangeAsync(auths);
        await authContext.SaveChangesAsync();

        metricsService.IncrementCounter("seeding.database_save_completed");
        metricsService.IncrementCounter("seeding.auth_seed_completed");
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
