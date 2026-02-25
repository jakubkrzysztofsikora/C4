using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace C4.Shared.Infrastructure.Persistence;

public static class DatabaseMigrator
{
    public static async Task MigrateAsync<TContext>(IServiceProvider serviceProvider, CancellationToken cancellationToken = default) where TContext : DbContext
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<TContext>>();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        var contextName = typeof(TContext).Name;

        if (context.Database.IsInMemory())
        {
            logger.LogInformation("Skipping migrations for {Context} (InMemory provider)", contextName);
            return;
        }

        logger.LogInformation("Applying migrations for {Context}", contextName);
        await context.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied for {Context}", contextName);
    }
}
