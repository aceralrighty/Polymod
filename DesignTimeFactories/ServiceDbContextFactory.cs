using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.ServiceModule.Data;

namespace TBD.DesignTimeFactories;

public class ServiceDbContextFactory : IDesignTimeDbContextFactory<ServiceDbContext>
{
    public ServiceDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true).Build();
        var connectionString = configuration.GetConnectionString("ServiceDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'ServiceDb' not found.");
        }

        var ob = new DbContextOptionsBuilder<ServiceDbContext>();
        ob.UseSqlServer(connectionString);
        return new ServiceDbContext(ob.Options);
    }
}