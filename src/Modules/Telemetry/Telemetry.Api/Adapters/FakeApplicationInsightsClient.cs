using C4.Modules.Telemetry.Application.Ports;

namespace C4.Modules.Telemetry.Api.Adapters;

public sealed class FakeApplicationInsightsClient : IApplicationInsightsClient
{
    public Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var records = new ApplicationInsightsHealthRecord[]
        {
            new("frontend", 0.94, now.AddMinutes(-5), RequestRate: 120, ErrorRate: 0.01, P95LatencyMs: 220),
            new("api", 0.79, now.AddMinutes(-4), RequestRate: 65, ErrorRate: 0.03, P95LatencyMs: 640),
            new("worker", 0.47, now.AddMinutes(-3), RequestRate: 22, ErrorRate: 0.12, P95LatencyMs: 2100)
        };

        return Task.FromResult<IReadOnlyCollection<ApplicationInsightsHealthRecord>>(records);
    }
}
