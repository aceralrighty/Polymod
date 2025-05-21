using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TBD.AddressService.Data;

namespace TBD.DesignTimeFactories
{
    public class AddressDbContextFactory : IDesignTimeDbContextFactory<AddressDbContext>
    {
        public AddressDbContext CreateDbContext(string[] args)
        {
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory()) // assumes root folder
                .AddJsonFile("appsettings.Development.json") // or "appsettings.json"
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AddressDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("AddressDb"));

            return new AddressDbContext(optionsBuilder.Options);
        }
    }
}