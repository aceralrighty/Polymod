using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.ScheduleModule.Data;

namespace TBD.DesignTimeFactories;

public class ScheduleDbContextFactory : IDesignTimeDbContextFactory<ScheduleDbContext>
{
    public ScheduleDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // Adjust if needed
            .AddJsonFile("appsettings.Development.json") // or just "appsettings.json"
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ScheduleDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("ScheduleDb"));

        return new ScheduleDbContext(optionsBuilder.Options);
    }
}