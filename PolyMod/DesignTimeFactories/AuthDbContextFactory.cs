using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.AuthModule.Data;

namespace TBD.DesignTimeFactories;

public class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("AuthDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'AuthDb' not found.");
        }

        var optionBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionBuilder.UseSqlServer(connectionString);

        return new AuthDbContext(optionBuilder.Options);
    }
}
