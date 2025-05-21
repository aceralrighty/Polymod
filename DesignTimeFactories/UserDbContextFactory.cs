using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.UserModule.Data;

namespace TBD.DesignTimeFactories;

public class UserDbContextFactory : IDesignTimeDbContextFactory<UserDbContext>
{
    public UserDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();
        var connectionString = configuration.GetConnectionString("UserDb");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Connection string 'UserDb' not found.");
        var optionsBuilder = new DbContextOptionsBuilder<UserDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new UserDbContext(optionsBuilder.Options);
    }
}