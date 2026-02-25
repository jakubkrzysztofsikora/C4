using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace C4.Modules.Telemetry.Infrastructure.Persistence;

public sealed class TelemetryDbContextFactory : IDesignTimeDbContextFactory<TelemetryDbContext>
{
    public TelemetryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TelemetryDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=c4_telemetry;Username=c4;Password=c4dev");
        return new TelemetryDbContext(optionsBuilder.Options);
    }
}
