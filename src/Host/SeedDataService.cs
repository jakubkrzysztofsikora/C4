using C4.Modules.Discovery.Infrastructure.Persistence;
using C4.Modules.Feedback.Infrastructure.Persistence;
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
        await DatabaseMigrator.MigrateAsync<FeedbackDbContext>(app.Services);

        await BackfillProjectMembershipsAsync(app.Services);

        if (app.Environment.IsDevelopment())
        {
            await SeedDevelopmentDataAsync(app.Services);
        }
    }

    private static async Task BackfillProjectMembershipsAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        var projects = await context.Projects.ToListAsync();
        var users = await context.Users.ToListAsync();

        foreach (var project in projects)
        {
            var firstUser = users.FirstOrDefault();
            if (firstUser is null) continue;

            var correctExternalUserId = firstUser.Id.Value.ToString();
            var hasValidMember = await context.Members.AnyAsync(m =>
                m.ProjectId == project.Id && m.ExternalUserId == correctExternalUserId);
            if (hasValidMember) continue;

            var badMembers = await context.Members
                .Where(m => m.ProjectId == project.Id && m.ExternalUserId != correctExternalUserId)
                .ToListAsync();
            context.Members.RemoveRange(badMembers);

            var member = C4.Modules.Identity.Domain.Member.Member.Invite(
                project.Id,
                correctExternalUserId,
                C4.Modules.Identity.Domain.Member.Role.Owner);
            context.Members.Add(member);
        }

        await context.SaveChangesAsync();
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
