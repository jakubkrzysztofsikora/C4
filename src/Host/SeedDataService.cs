using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Graph.Infrastructure.Persistence;
using C4.Modules.Identity.Application.Ports;
using C4.Modules.Identity.Infrastructure.Persistence;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using C4.Modules.Visualization.Infrastructure.Persistence;
using C4.Shared.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Host;

public static class SeedDataService
{
    public static async Task MigrateAndSeedAsync(WebApplication app)
    {
        await DatabaseMigrator.MigrateAsync<IdentityDbContext>(app.Services);
        await DatabaseMigrator.MigrateAsync<DiscoveryDbContext>(app.Services);
        await DatabaseMigrator.MigrateAsync<GraphDbContext>(app.Services);
        await DatabaseMigrator.MigrateAsync<TelemetryDbContext>(app.Services);
        await DatabaseMigrator.MigrateAsync<VisualizationDbContext>(app.Services);

        if (app.Environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(app.Services);
        }
    }

    private static async Task SeedDevelopmentDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        await SeedIdentityAsync(scope);
        await SeedVisualizationAsync(scope);
    }

    private static async Task SeedIdentityAsync(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        if (!await context.Organizations.AnyAsync())
        {
            var orgResult = C4.Modules.Identity.Domain.Organization.Organization.Create("C4 Demo Organization");
            if (orgResult.IsSuccess)
            {
                var org = orgResult.Value;
                context.Organizations.Add(org);
                await context.SaveChangesAsync();

                var projectResult = C4.Modules.Identity.Domain.Project.Project.Create(org.Id, "Sample Cloud Project");
                if (projectResult.IsSuccess)
                {
                    context.Projects.Add(projectResult.Value);
                    await context.SaveChangesAsync();
                }
            }
        }

        if (!await context.Users.AnyAsync())
        {
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
            string passwordHash = passwordHasher.Hash("Password123!");
            var demoUser = C4.Modules.Identity.Domain.User.User.Create("demo@c4.local", passwordHash, "Demo User");
            context.Users.Add(demoUser);
            await context.SaveChangesAsync();
        }
    }

    private static async Task SeedVisualizationAsync(IServiceScope scope)
    {
        var context = scope.ServiceProvider.GetRequiredService<VisualizationDbContext>();

        if (await context.ViewPresets.AnyAsync())
            return;

        var preset = C4.Modules.Visualization.Domain.Preset.ViewPreset.Create(
            Guid.Empty,
            "Default View",
            """{"layout":"hierarchical","showLabels":true,"theme":"light"}""");

        context.ViewPresets.Add(preset);
        await context.SaveChangesAsync();
    }
}
