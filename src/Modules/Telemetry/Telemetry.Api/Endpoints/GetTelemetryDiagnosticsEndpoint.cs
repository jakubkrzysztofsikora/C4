using C4.Modules.Telemetry.Application.Ports;
using C4.Shared.Infrastructure.Endpoints;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using Microsoft.Extensions.Configuration;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class GetTelemetryDiagnosticsEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/telemetry/diagnostics", async (
            Guid projectId,
            IAppInsightsConfigStore configStore,
            IConfiguration configuration,
            IProjectAuthorizationService authorizationService,
            IApplicationInsightsTokenProvider? tokenProvider,
            CancellationToken ct) =>
        {
            var auth = await authorizationService.AuthorizeAsync(projectId, ct);
            if (!auth.IsSuccess) return Results.Forbid();

            var config = await configStore.GetAsync(projectId, ct);
            var targets = await configStore.GetTargetsAsync(projectId, ct);

            var hasProjectApiKey = !string.IsNullOrWhiteSpace(config?.ApiKey);
            var hasLegacyKey = !string.IsNullOrWhiteSpace(config?.InstrumentationKey);
            var hasGlobalApiKey = !string.IsNullOrWhiteSpace(configuration["ApplicationInsights:ApiKey"]);
            var hasGlobalAppId = !string.IsNullOrWhiteSpace(configuration["ApplicationInsights:AppId"]);
            var hasTokenProvider = tokenProvider is not null;

            string resolvedAuthMethod;
            if (hasProjectApiKey)
                resolvedAuthMethod = "project-api-key";
            else if (hasLegacyKey && !IsLikelyInstrumentationKey(config!.InstrumentationKey))
                resolvedAuthMethod = "legacy-key";
            else if (hasGlobalApiKey)
                resolvedAuthMethod = "global-api-key";
            else if (hasTokenProvider)
                resolvedAuthMethod = "token-provider";
            else
                resolvedAuthMethod = "none";

            var issues = new List<string>();
            if (targets.Count == 0 && !hasGlobalAppId)
                issues.Add("No AppId(s) configured for this project or globally");
            if (resolvedAuthMethod == "none")
                issues.Add("No authentication material available (no API key, no token provider)");
            if (hasLegacyKey && IsLikelyInstrumentationKey(config!.InstrumentationKey))
                issues.Add("Legacy InstrumentationKey field contains an instrumentation key (GUID), not an API key");

            return Results.Ok(new TelemetryDiagnosticsResponse(
                projectId,
                targets.Count,
                hasProjectApiKey,
                hasGlobalApiKey,
                hasTokenProvider,
                resolvedAuthMethod,
                issues.Count == 0,
                issues,
                DateTime.UtcNow));
        }).RequireAuthorization();
    }

    private static bool IsLikelyInstrumentationKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return Guid.TryParse(value, out _)
               || value.Contains("InstrumentationKey=", StringComparison.OrdinalIgnoreCase);
    }

    public sealed record TelemetryDiagnosticsResponse(
        Guid ProjectId,
        int ConfiguredTargetCount,
        bool HasProjectApiKey,
        bool HasGlobalApiKey,
        bool HasTokenProvider,
        string ResolvedAuthMethod,
        bool IsHealthy,
        IReadOnlyCollection<string> Issues,
        DateTime GeneratedAtUtc);
}
