using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Graph.Infrastructure.Persistence;
using C4.Modules.Identity.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Visualization.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Shared.Testing;

public sealed class C4WebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbSuffix = Guid.NewGuid().ToString("N")[..8];

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            ReplaceDbContext<IdentityDbContext>(services, $"identity-{_dbSuffix}");
            ReplaceDbContext<DiscoveryDbContext>(services, $"discovery-{_dbSuffix}");
            ReplaceDbContext<GraphDbContext>(services, $"graph-{_dbSuffix}");
            ReplaceDbContext<TelemetryDbContext>(services, $"telemetry-{_dbSuffix}");
            ReplaceDbContext<VisualizationDbContext>(services, $"visualization-{_dbSuffix}");

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        });
    }

    private static void ReplaceDbContext<TContext>(IServiceCollection services, string dbName) where TContext : DbContext
    {
        var descriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>) || d.ServiceType == typeof(TContext))
            .ToList();

        foreach (var d in descriptors)
            services.Remove(d);

        services.AddDbContext<TContext>(options => options.UseInMemoryDatabase(dbName));
    }
}
