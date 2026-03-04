namespace C4.Modules.Telemetry.Application.Ports;

public interface IAppInsightsConfigStore
{
    Task StoreAsync(Guid projectId, string appId, string instrumentationKey, CancellationToken cancellationToken);
    Task<AppInsightsConfig?> GetAsync(Guid projectId, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<string>> GetTargetsAsync(Guid projectId, CancellationToken cancellationToken);
    Task SaveTargetsAsync(Guid projectId, IReadOnlyCollection<string> appIds, string instrumentationKey, CancellationToken cancellationToken);
}

public sealed record AppInsightsConfig(Guid ProjectId, string AppId, string InstrumentationKey);
