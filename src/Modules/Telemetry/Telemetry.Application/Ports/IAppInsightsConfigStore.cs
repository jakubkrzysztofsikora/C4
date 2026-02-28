namespace C4.Modules.Telemetry.Application.Ports;

public interface IAppInsightsConfigStore
{
    Task StoreAsync(Guid projectId, string appId, string instrumentationKey, CancellationToken cancellationToken);
    Task<AppInsightsConfig?> GetAsync(Guid projectId, CancellationToken cancellationToken);
}

public sealed record AppInsightsConfig(Guid ProjectId, string AppId, string InstrumentationKey);
