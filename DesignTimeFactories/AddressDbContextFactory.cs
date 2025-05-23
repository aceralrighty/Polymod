using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.AddressModule.Data;

namespace TBD.DesignTimeFactories
{
    public class AddressDbContextFactory : IDesignTimeDbContextFactory<AddressDbContext>
    {
        public AddressDbContext CreateDbContext(string[] args)
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile($"appsettings.{environment}.json", optional: true)
                .Build();

            var connectionString = configuration.GetConnectionString("AddressDb");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'AddressDb' not found.");

            var optionsBuilder = new DbContextOptionsBuilder<AddressDbContext>();
            optionsBuilder.UseSqlServer(connectionString);

            return new AddressDbContext(optionsBuilder.Options);
        }
    }
}