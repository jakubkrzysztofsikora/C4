using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain;

namespace C4.Modules.Telemetry.Infrastructure.Repositories;

internal sealed class TelemetryTargetStore(IAppInsightsConfigStore configStore) : ITelemetryTargetStore
{
    public async Task<IReadOnlyCollection<TelemetryTarget>> GetTargetsAsync(
        Guid projectId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> appIds = await configStore.GetTargetsAsync(projectId, cancellationToken);

        return appIds
            .Select(appId => new TelemetryTarget(
                appId,
                TelemetryProvider.ApplicationInsights,
                AuthMode.ApiKey,
                new Dictionary<string, string> { ["appId"] = appId }))
            .ToArray();
    }

    public async Task UpsertTargetAsync(
        Guid projectId,
        TelemetryTarget target,
        CancellationToken cancellationToken)
    {
        target.ConnectionMetadata.TryGetValue("appId", out string? appId);
        string resolvedAppId = appId ?? target.Id;
        string apiKey = target.ConnectionMetadata.GetValueOrDefault("apiKey", string.Empty);

        string instrumentationKey = target.ConnectionMetadata.GetValueOrDefault("instrumentationKey", string.Empty);
        await configStore.StoreAsync(projectId, resolvedAppId, instrumentationKey, apiKey, cancellationToken);
    }

    public async Task DeleteTargetAsync(
        Guid projectId,
        string targetId,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> existing = await configStore.GetTargetsAsync(projectId, cancellationToken);
        if (existing.Count == 0)
            return;

        string[] filtered = existing
            .Where(appId => !appId.Equals(targetId, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        AppInsightsConfig? config = await configStore.GetAsync(projectId, cancellationToken);
        await configStore.SaveTargetsAsync(projectId, filtered, config?.InstrumentationKey ?? string.Empty, cancellationToken);
    }
}
