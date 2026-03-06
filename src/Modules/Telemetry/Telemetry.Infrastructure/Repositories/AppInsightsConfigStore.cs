using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Telemetry.Infrastructure.Repositories;

public sealed class AppInsightsConfigStore(TelemetryDbContext dbContext) : IAppInsightsConfigStore
{
    public async Task StoreAsync(Guid projectId, string appId, string instrumentationKey, string apiKey, CancellationToken cancellationToken)
    {
        var incomingTargets = ParseTargets(appId);
        var serializedIncoming = SerializeTargets(incomingTargets);
        var normalizedInstrumentationKey = instrumentationKey.Trim();
        var normalizedApiKey = apiKey.Trim();

        var existing = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

        if (existing is not null)
        {
            var merged = ParseTargets(existing.AppId);
            foreach (var target in incomingTargets)
            {
                merged.Add(target);
            }

            existing.AppId = SerializeTargets(merged);
            if (!string.IsNullOrWhiteSpace(normalizedInstrumentationKey))
                existing.InstrumentationKey = normalizedInstrumentationKey;
            if (!string.IsNullOrWhiteSpace(normalizedApiKey))
                existing.ApiKey = normalizedApiKey;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            await dbContext.AppInsightsConfigs.AddAsync(new AppInsightsConfigEntity
            {
                ProjectId = projectId,
                AppId = serializedIncoming,
                InstrumentationKey = normalizedInstrumentationKey,
                ApiKey = normalizedApiKey,
                UpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetTargetsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);
        if (entity is null)
            return [];

        return ParseTargets(entity.AppId).OrderBy(v => v, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public async Task SaveTargetsAsync(
        Guid projectId,
        IReadOnlyCollection<string> appIds,
        string instrumentationKey,
        CancellationToken cancellationToken)
    {
        var serialized = SerializeTargets(appIds);
        var existing = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

        if (existing is null)
        {
            await dbContext.AppInsightsConfigs.AddAsync(new AppInsightsConfigEntity
            {
                ProjectId = projectId,
                AppId = serialized,
                InstrumentationKey = instrumentationKey,
                UpdatedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            existing.AppId = serialized;
            if (!string.IsNullOrWhiteSpace(instrumentationKey))
                existing.InstrumentationKey = instrumentationKey;
            existing.UpdatedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AppInsightsConfig?> GetAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var entity = await dbContext.AppInsightsConfigs.FirstOrDefaultAsync(c => c.ProjectId == projectId, cancellationToken);

        if (entity is null)
            return null;

        return new AppInsightsConfig(entity.ProjectId, entity.AppId, entity.InstrumentationKey, entity.ApiKey);
    }

    private static HashSet<string> ParseTargets(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return [];

        var parts = value
            .Split([';', ',', '\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !string.IsNullOrWhiteSpace(part));

        return new HashSet<string>(parts, StringComparer.OrdinalIgnoreCase);
    }

    private static string SerializeTargets(IEnumerable<string> values)
        => string.Join(';', values
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v, StringComparer.OrdinalIgnoreCase));
}
