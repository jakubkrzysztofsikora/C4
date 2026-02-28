using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Infrastructure.Repositories;

public sealed class AppInsightsConfigStore(TelemetryDbContext dbContext) : IAppInsightsConfigStore
{
    public async Task StoreAsync(Guid projectId, string appId, string instrumentationKey, CancellationToken cancellationToken)
    {
        var existing = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

        if (existing is not null)
        {
            existing.AppId = appId;
            existing.InstrumentationKey = instrumentationKey;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            await dbContext.AppInsightsConfigs.AddAsync(new AppInsightsConfigEntity
            {
                ProjectId = projectId,
                AppId = appId,
                InstrumentationKey = instrumentationKey,
                UpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AppInsightsConfig?> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

        if (entity is null)
            return null;

        return new AppInsightsConfig(entity.ProjectId, entity.AppId, entity.InstrumentationKey);
    }
}
