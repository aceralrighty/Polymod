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

        var hasher = new Hasher();

        var auth1 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "joe.rogan@example.com",
            HashedPassword = hasher.HashPassword("DMTBears"),
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
            Email = "Ari-the-jew@cabal.com",
            HashedPassword = hasher.HashPassword("PissInBottles"),
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
            Email = "Shane.Downey_gillis@drunk.com",
            HashedPassword = hasher.HashPassword("BudLightSucks"),
            Username = "FootballIsLife",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(15),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(5),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(20),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(10)
        };

        var auth4 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "bryan.callen@suspicion.com",
            HashedPassword = hasher.HashPassword("ItsEntirelyPossible"),
            Username = "TheFighter",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(48),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(12),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(15),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(25),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(5)
        };

        var auth5 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "brendan.schaub@redacted.com",
            HashedPassword = hasher.HashPassword("GadooshBapa"),
            Username = "ThickBoy",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(32),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(20),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(30),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(35),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(2)
        };

        var auth6 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "eddie.bravo@flatearth.com",
            HashedPassword = hasher.HashPassword("JiuJitsuTruth"),
            Username = "10thPlanet",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(64),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(45),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(50),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(1)
        };

        var auth7 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "joey.diaz@coco.com",
            HashedPassword = hasher.HashPassword("TremendousStars"),
            Username = "UncleJoey",
            FailedLoginAttempts = 5,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(60),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(65),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(14)
        };

        var auth8 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "tom.segura@ymh.com",
            HashedPassword = hasher.HashPassword("MommyJeans"),
            Username = "TopDog",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(40),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(18),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(25),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(28),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(3)
        };

        var auth9 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "christina.p@watermom.com",
            HashedPassword = hasher.HashPassword("HighAndTight"),
            Username = "MainMommy",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(56),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(22),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(18),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(24),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(6)
        };

        var auth10 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "bert.kreischer@machine.com",
            HashedPassword = hasher.HashPassword("ShirtlessComedy"),
            Username = "TheMachine",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(14),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(40),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(42),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(8),
            DeletedAt = DateTime.UtcNow + TimeSpan.FromDays(10)
        };

        var auth11 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "duncan.trussell@midnight.com",
            HashedPassword = hasher.HashPassword("CosmicLove"),
            Username = "MidnightGospel",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(72),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(25),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(35),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(38),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(4)
        };

        var auth12 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "andrew.santino@redhair.com",
            HashedPassword = hasher.HashPassword("CheetoFingers"),
            Username = "RedRocket",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(44),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(11),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(22),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(26),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(9)
        };

        var auth13 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "bobby.lee@tigerbelly.com",
            HashedPassword = hasher.HashPassword("BadFriends"),
            Username = "Nosotros",
            FailedLoginAttempts = 4,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(8),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(50),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(52),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(12)
        };

        var auth14 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "theo.von@rat.com",
            HashedPassword = hasher.HashPassword("DarkArts"),
            Username = "KingRat",
            FailedLoginAttempts = 1,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(52),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(16),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(28),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(31),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(7)
        };

        var auth15 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "whitney.cummings@robot.com",
            HashedPassword = hasher.HashPassword("GoodForYou"),
            Username = "Robutney",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(60),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(21),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(33),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(36),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(5)
        };

        var auth16 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "tim.dillon@pigpen.com",
            HashedPassword = hasher.HashPassword("FakeNews"),
            Username = "PigKing",
            FailedLoginAttempts = 2,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(13),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(17),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(19),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(11)
        };

        var auth17 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "dave.smith@komedy.com",
            HashedPassword = hasher.HashPassword("PartOfTheProblem"),
            Username = "BigJayOakerson",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(38),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(19),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(41),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(44),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(2)
        };

        var auth18 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "mark.normand@tuesdays.com",
            HashedPassword = hasher.HashPassword("WeAreGay"),
            Username = "Queef",
            FailedLoginAttempts = 3,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(46),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(10),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(26),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(29),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(13)
        };

        var auth19 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "sam.tripoli@tinfoil.com",
            HashedPassword = hasher.HashPassword("BrokenSimulation"),
            Username = "TinfoilHat",
            FailedLoginAttempts = 6,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(5),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(55),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(58),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(16)
        };

        var auth20 = new AuthUser
        {
            Id = Guid.NewGuid(),
            Email = "alex.jones@infowars.com",
            HashedPassword = hasher.HashPassword("TurningFrogsGay"),
            Username = "InfoWarrior",
            FailedLoginAttempts = 0,
            RefreshToken = JwtTokenGenerator.GenerateJwtToken(80),
            RefreshTokenExpiry = DateTime.UtcNow.AddDays(30),
            CreatedAt = DateTime.UtcNow + TimeSpan.FromDays(100),
            UpdatedAt = DateTime.UtcNow + TimeSpan.FromDays(105),
            LastLogin = DateTime.UtcNow + TimeSpan.FromDays(1)
        };

        // Add all users to the list
        auths.AddRange([
            auth1, auth2, auth3, auth4, auth5, auth6, auth7, auth8, auth9, auth10, auth11, auth12, auth13, auth14,
            auth15, auth16, auth17, auth18, auth19, auth20
        ]);

        await authContext.AuthUsers.AddRangeAsync(auths);
        await authContext.SaveChangesAsync();
    }
}
