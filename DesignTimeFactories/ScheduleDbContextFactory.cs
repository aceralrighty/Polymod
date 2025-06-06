using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.ScheduleModule.Data;

namespace TBD.DesignTimeFactories
{
    public class ScheduleDbContextFactory : IDesignTimeDbContextFactory<ScheduleDbContext>
    {
        public ScheduleDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("ScheduleDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'ScheduleDb' not found.");

            var optionsBuilder = new DbContextOptionsBuilder<ScheduleDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new ScheduleDbContext(optionsBuilder.Options);
        }
    }
}
