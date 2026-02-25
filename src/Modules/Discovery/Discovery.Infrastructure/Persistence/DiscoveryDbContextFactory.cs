using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace C4.Modules.Discovery.Infrastructure.Persistence;

public sealed class DiscoveryDbContextFactory : IDesignTimeDbContextFactory<DiscoveryDbContext>
{
    public DiscoveryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DiscoveryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=c4_discovery;Username=c4;Password=c4dev");
        return new DiscoveryDbContext(optionsBuilder.Options);
    }
}
