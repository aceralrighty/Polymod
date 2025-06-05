using TBD.AuthModule.Data;
using TBD.AuthModule.Models;
using TBD.Shared.Utils;

namespace TBD.Data.Seeding;

public static class AuthSeeder
{
    public static async Task ReseedSeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var authContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();

        await authContext.Database.EnsureDeletedAsync();
        await authContext.Database.EnsureCreatedAsync();
        await authContext.SaveChangesAsync();

        await SeedAuthAsync(authContext);
    }

    private static async Task SeedAuthAsync(AuthDbContext authContext)
    {
        var auths = new List<AuthUser>();

        var auth1 = new AuthUser
        {
            Id = Guid.NewGuid(),
            AuthId = Guid.NewGuid(),
            Email = "joe.rogan@example.com",
            HashedPassword = Hasher.HashPassword("DMTBears"),
            Username = "freakBitches",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(20),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(30),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(30)
        };
        var auth2 = new AuthUser
        {
            Id = Guid.NewGuid(),
            AuthId = Guid.NewGuid(),
            Email = "Ari-the-jew@cabal.com",
            HashedPassword = Hasher.HashPassword("PissInBottles"),
            Username = "SalviaLoser",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(10),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(30),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(20)
        };
        var auth3 = new AuthUser
        {
            Id = Guid.NewGuid(),
            AuthId = Guid.NewGuid(),
            Email = "Shane.Downey_gillis@drunk.com",
            HashedPassword = Hasher.HashPassword("BudLightSucks"),
            Username = "FootballIsLife",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(5),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(20),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(10)
        };
        auths.Add(auth1);
        auths.Add(auth2);
        auths.Add(auth3);
        await authContext.AuthUsers.AddRangeAsync(auths);
        await authContext.SaveChangesAsync();
    }
}
