using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.RecommendationModule.Data;

namespace TBD.DesignTimeFactories;

public class RecommendationDbContextFactory : IDesignTimeDbContextFactory<RecommendationDbContext>
{
    public RecommendationDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true).Build();
        var connectionString = configuration.GetConnectionString("RecDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'RecDb' not found.");
        }

        var optionsBuilder = new DbContextOptionsBuilder<RecommendationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);
        return new RecommendationDbContext(optionsBuilder.Options);
    }
}
