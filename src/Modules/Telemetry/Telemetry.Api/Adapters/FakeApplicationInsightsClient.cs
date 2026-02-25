using C4.Modules.Telemetry.Application.Ports;

namespace C4.Modules.Telemetry.Api.Adapters;

public sealed class FakeApplicationInsightsClient : IApplicationInsightsClient
{
    public Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var records = new ApplicationInsightsHealthRecord[]
        {
            new("frontend", 0.94, now.AddMinutes(-5)),
            new("api", 0.79, now.AddMinutes(-4)),
            new("worker", 0.47, now.AddMinutes(-3))
        };

        return Task.FromResult<IReadOnlyCollection<ApplicationInsightsHealthRecord>>(records);
    }
}
