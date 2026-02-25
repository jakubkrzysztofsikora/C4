using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace C4.Modules.Graph.Infrastructure.Persistence;

public sealed class GraphDbContextFactory : IDesignTimeDbContextFactory<GraphDbContext>
{
    public GraphDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<GraphDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=c4_graph;Username=c4;Password=c4dev");
        return new GraphDbContext(optionsBuilder.Options);
    }
}
