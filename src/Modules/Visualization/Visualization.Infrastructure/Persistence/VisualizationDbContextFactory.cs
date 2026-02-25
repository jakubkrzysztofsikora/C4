using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace C4.Modules.Visualization.Infrastructure.Persistence;

public sealed class VisualizationDbContextFactory : IDesignTimeDbContextFactory<VisualizationDbContext>
{
    public VisualizationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VisualizationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=c4_visualization;Username=c4;Password=c4dev");
        return new VisualizationDbContext(optionsBuilder.Options);
    }
}
