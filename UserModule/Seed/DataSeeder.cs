using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TBD.AddressModule.Data;
using TBD.AddressModule.Models;
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

        // Clean DB
        await userContext.Database.EnsureDeletedAsync();
        await addressContext.Database.EnsureDeletedAsync();

        // Migrate in order
        await userContext.Database.MigrateAsync();

        try
        {
            await addressContext.Database.MigrateAsync();
        }
        catch (SqlException ex) when (ex.Number == 2714)
        {
            Console.WriteLine("Address context tables already exist, skipping migration");
        }

        await SeedUsersAsync(userContext);
        await SeedUserAddressesAsync(addressContext, userContext);
    }


    private static async Task SeedUsersAsync(UserDbContext context)
    {
        var baseDate = DateTime.UtcNow;
        var hasher = new Hasher();
        var users = new List<User>
        {
            // Standard users with various creation patterns
            new()
            {
                Id = Guid.NewGuid(),
                Username = "john.doe",
                Email = "john.doe@example.com",
                Password = hasher.HashPassword("SecurePass123!"),
                CreatedAt = baseDate.AddDays(-365), // Old user
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
                CreatedAt = baseDate.AddDays(-730), // Very old admin
                UpdatedAt = baseDate.AddDays(-1), // Recently active
                Schedule = new Schedule()
            },

            // Users with international/diverse backgrounds
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

            // Edge case users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "test.user.with.long.name",
                Email = "very.long.email.address.for.testing@extremelylongdomainname.international",
                Password = hasher.HashPassword("VeryLongPasswordWithSpecialChars!@#$%^&*()"),
                CreatedAt = baseDate.AddMinutes(-30), // Very recent user
                UpdatedAt = baseDate.AddMinutes(-30),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "a", // Minimal username
                Email = "a@b.co", // Minimal email
                Password = hasher.HashPassword("Short1!"),
                CreatedAt = baseDate.AddDays(-1),
                UpdatedAt = baseDate.AddDays(-1),
                Schedule = new Schedule()
            },

            // Users with special characters and numbers
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
            },

            // Inactive/old users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "inactive.old.user",
                Email = "old.user@defunct-company.com",
                Password = hasher.HashPassword("OldPassword999"),
                CreatedAt = baseDate.AddDays(-1095), // 3 years old
                UpdatedAt = baseDate.AddDays(-1000), // Last updated 2.7 years ago
                Schedule = new Schedule()
            },

            // Corporate/business users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "corp.admin",
                Email = "admin@fortune500company.com",
                Password = hasher.HashPassword("Corporate123!@#"),
                CreatedAt = baseDate.AddDays(-200),
                UpdatedAt = baseDate.AddHours(-6),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "sales.manager",
                Email = "sales@startup.io",
                Password = hasher.HashPassword("SalesForce456$"),
                CreatedAt = baseDate.AddDays(-75),
                UpdatedAt = baseDate.AddDays(-3),
                Schedule = new Schedule()
            },

            // Students/academic users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "student.2024",
                Email = "student2024@university.edu",
                Password = hasher.HashPassword("StudyHard789!"),
                CreatedAt = baseDate.AddDays(-240), // Started academic year
                UpdatedAt = baseDate.AddDays(-12),
                Schedule = new Schedule()
            },
            new()
            {
                Id = Guid.NewGuid(),
                Username = "professor.smith",
                Email = "prof.smith@academy.edu",
                Password = hasher.HashPassword("Academic2024#"),
                CreatedAt = baseDate.AddDays(-500),
                UpdatedAt = baseDate.AddDays(-8),
                Schedule = new Schedule()
            },

            // Government/official users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "gov.official",
                Email = "official@government.gov",
                Password = hasher.HashPassword("Official123Gov!"),
                CreatedAt = baseDate.AddDays(-400),
                UpdatedAt = baseDate.AddDays(-25),
                Schedule = new Schedule()
            },

            // Healthcare users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "dr.johnson",
                Email = "dr.johnson@hospital.health",
                Password = hasher.HashPassword("Medical456!"),
                CreatedAt = baseDate.AddDays(-300),
                UpdatedAt = baseDate.AddDays(-4),
                Schedule = new Schedule()
            },

            // Freelancer/contractor users
            new()
            {
                Id = Guid.NewGuid(),
                Username = "freelancer.dev",
                Email = "contact@freelancer-portfolio.com",
                Password = hasher.HashPassword("FreelanceLife789$"),
                CreatedAt = baseDate.AddDays(-150),
                UpdatedAt = baseDate.AddDays(-6),
                Schedule = new Schedule()
            },

            // Social media influencer
            new()
            {
                Id = Guid.NewGuid(),
                Username = "influencer_star",
                Email = "business@influencer-agency.net",
                Password = hasher.HashPassword("Influence2024!"),
                CreatedAt = baseDate.AddDays(-80),
                UpdatedAt = baseDate.AddHours(-12),
                Schedule = new Schedule()
            },

            // Retiree user
            new()
            {
                Id = Guid.NewGuid(),
                Username = "retiree.smith",
                Email = "retirement.life@senior.com",
                Password = hasher.HashPassword("GoldenYears123!"),
                CreatedAt = baseDate.AddDays(-600),
                UpdatedAt = baseDate.AddDays(-45),
                Schedule = new Schedule()
            }
        };

        await context.Set<User>().AddRangeAsync(users);
        await context.SaveChangesAsync();

        Console.WriteLine($"Seeded {users.Count} users");
    }

    private static async Task SeedUserAddressesAsync(AddressDbContext addressContext, UserDbContext context)
    {
        var users = await context.Set<User>().ToListAsync();
        if (users.Count == 0)
        {
            Console.WriteLine("No users found for address seeding");
            return;
        }

        var addresses = new List<UserAddress>();

        // Comprehensive address variations - ALL ZIP CODES AS STRINGS
        var addressData = new[]
        {
            // Standard US addresses
            new
            {
                Address1 = "123 Main Street",
                Address2 = "Apt 101",
                City = "Seattle",
                State = "WA",
                ZipCode = "98101"
            },
            new
            {
                Address1 = "456 Market Street",
                Address2 = (string)null,
                City = "San Francisco",
                State = "CA",
                ZipCode = "94103"
            },
            new
            {
                Address1 = "789 Broadway",
                Address2 = "Suite 200",
                City = "New York",
                State = "NY",
                ZipCode = "10001"
            },

            // Diverse US locations
            new
            {
                Address1 = "2150 Peachtree Road NE",
                Address2 = "Unit 15B",
                City = "Atlanta",
                State = "GA",
                ZipCode = "30309"
            },
            new
            {
                Address1 = "8901 Sunset Boulevard",
                Address2 = (string)null,
                City = "Los Angeles",
                State = "CA",
                ZipCode = "90069"
            },
            new
            {
                Address1 = "1200 S Michigan Avenue",
                Address2 = "Floor 23",
                City = "Chicago",
                State = "IL",
                ZipCode = "60605"
            },
            new
            {
                Address1 = "3456 Ocean Drive",
                Address2 = "Penthouse",
                City = "Miami",
                State = "FL",
                ZipCode = "33139"
            },
            new
            {
                Address1 = "567 Congress Street",
                Address2 = (string)null,
                City = "Austin",
                State = "TX",
                ZipCode = "78701"
            },

            // Rural addresses
            new
            {
                Address1 = "Rural Route 2 Box 45",
                Address2 = (string)null,
                City = "Smalltown",
                State = "IA",
                ZipCode = "50001"
            },
            new
            {
                Address1 = "HC 65 Box 12",
                Address2 = (string)null,
                City = "Mountain View",
                State = "WY",
                ZipCode = "82414"
            },

            // PO Box addresses
            new
            {
                Address1 = "PO Box 1234",
                Address2 = (string)null,
                City = "Phoenix",
                State = "AZ",
                ZipCode = "85001"
            },
            new
            {
                Address1 = "P.O. Box 567",
                Address2 = (string)null,
                City = "Portland",
                State = "OR",
                ZipCode = "97201"
            },

            // University addresses
            new
            {
                Address1 = "University Campus",
                Address2 = "Dormitory Hall Room 201",
                City = "College Station",
                State = "TX",
                ZipCode = "77843"
            },
            new
            {
                Address1 = "Graduate Housing Complex",
                Address2 = "Building C, Apt 4",
                City = "Berkeley",
                State = "CA",
                ZipCode = "94720"
            },

            // Military/Government
            new
            {
                Address1 = "Building 1234",
                Address2 = "Room 567",
                City = "Fort Knox",
                State = "KY",
                ZipCode = "40121"
            },
            new
            {
                Address1 = "Federal Building",
                Address2 = "Suite 890",
                City = "Washington",
                State = "DC",
                ZipCode = "20001"
            },

            // Unique/Special addresses
            new
            {
                Address1 = "1 Hacker Way",
                Address2 = (string)null,
                City = "Menlo Park",
                State = "CA",
                ZipCode = "94025"
            },
            new
            {
                Address1 = "Space Needle",
                Address2 = "Observation Deck",
                City = "Seattle",
                State = "WA",
                ZipCode = "98109"
            },
            new
            {
                Address1 = "Times Square",
                Address2 = "Billboard Location",
                City = "New York",
                State = "NY",
                ZipCode = "10036"
            },
            new
            {
                Address1 = "123 Memory Lane",
                Address2 = (string)null,
                City = "Hometown",
                State = "OH",
                ZipCode = "44001"
            },

            // ZIP codes that need leading zeros (common problem areas)
            new
            {
                Address1 = "100 State Street",
                Address2 = (string)null,
                City = "Boston",
                State = "MA",
                ZipCode = "02101" // Leading zero important
            },
            new
            {
                Address1 = "200 Liberty Street",
                Address2 = (string)null,
                City = "Hartford",
                State = "CT",
                ZipCode = "06101" // Leading zero important
            },
            new
            {
                Address1 = "300 Washington Ave",
                Address2 = (string)null,
                City = "Newark",
                State = "NJ",
                ZipCode = "07101" // Leading zero important
            },

            // ZIP+4 examples
            new
            {
                Address1 = "1600 Pennsylvania Avenue NW",
                Address2 = (string)null,
                City = "Washington",
                State = "DC",
                ZipCode = "20500-0003" // ZIP+4 format
            },
            new
            {
                Address1 = "350 Fifth Avenue",
                Address2 = (string)null,
                City = "New York",
                State = "NY",
                ZipCode = "10118-0110" // ZIP+4 format
            }
        };

        for (var i = 0; i < Math.Min(users.Count, addressData.Length); i++)
        {
            var addressInfo = addressData[i];
            var address = new UserAddress(
                userId: users[i].Id,
                user: users[i],
                address1: addressInfo.Address1,
                address2: addressInfo.Address2,
                city: addressInfo.City,
                state: addressInfo.State,
                zipCode: addressInfo.ZipCode) { Id = Guid.NewGuid() };

            addresses.Add(address);
        }

        // Add some users with multiple addresses (if there are remaining users)
        if (users.Count > addressData.Length)
        {
            var remainingUsers = users.Skip(addressData.Length).Take(3);
            foreach (var user in remainingUsers)
            {
                // Add work address
                addresses.Add(new UserAddress(
                    userId: user.Id,
                    user: user,
                    address1: "Work Plaza Building",
                    address2: $"Office {Random.Shared.Next(100, 999)}",
                    city: "Business City",
                    state: "CA",
                    zipCode: "90210") { Id = Guid.NewGuid() });

                // Add home address
                addresses.Add(new UserAddress(
                    userId: user.Id,
                    user: user,
                    address1: $"{Random.Shared.Next(1000, 9999)} Residential Ave",
                    address2: Random.Shared.Next(0, 2) == 0 ? null : $"Unit {Random.Shared.Next(1, 50)}",
                    city: "Suburbia",
                    state: "TX",
                    zipCode: "75001") { Id = Guid.NewGuid() });
            }
        }

        await addressContext.UserAddress.AddRangeAsync(addresses);
        await addressContext.SaveChangesAsync();

        Console.WriteLine($"Seeded {addresses.Count} addresses for {users.Count} users");
    }
}
