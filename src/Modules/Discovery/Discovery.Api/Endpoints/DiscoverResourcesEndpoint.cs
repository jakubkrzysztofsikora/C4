using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DiscoverResourcesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/discover", async (Guid subscriptionId, DiscoverResourcesRequest request, ISender sender, ILogger<DiscoverResourcesEndpoint> logger, CancellationToken ct) =>
        {
            if (!TryParseSources(request.Sources, out var parsedSources, out var invalidSources))
            {
                return Results.BadRequest(new DiscoverResourcesValidationErrorResponse(
                    "discovery.invalid_sources",
                    "One or more discovery sources are invalid. Use enum names or numeric values.",
                    "sources",
                    invalidSources,
                    Enum.GetNames<DiscoverySourceKind>()));
            }

            try
            {
                var result = await sender.Send(
                    new DiscoverResourcesCommand(
                        subscriptionId,
                        request.ExternalSubscriptionId,
                        request.ProjectId,
                        request.OrganizationId,
                        parsedSources),
                    ct);

                if (result.IsSuccess)
                {
                    return Results.Ok(result.Value);
                }

                var escalation = DiscoveryEscalationMapper.ForFailure(result.Error);
                return Results.BadRequest(new DiscoverResourcesErrorResponse(
                    result.Error.Code,
                    result.Error.Message,
                    escalation.Status,
                    escalation.EscalationLevel,
                    escalation.UserActionHint));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled discovery exception for project {ProjectId} and subscription {SubscriptionId}", request.ProjectId, subscriptionId);
                var mappedError = DiscoveryEscalationMapper.MapExternalFailure(ex);
                var escalation = DiscoveryEscalationMapper.ForFailure(mappedError);

                return Results.BadRequest(new DiscoverResourcesErrorResponse(
                    mappedError.Code,
                    mappedError.Message,
                    escalation.Status,
                    escalation.EscalationLevel,
                    escalation.UserActionHint));
            }
        })
        .RequireAuthorization();
    }

    private static bool TryParseSources(
        IReadOnlyCollection<JsonElement>? rawSources,
        out IReadOnlyCollection<DiscoverySourceKind>? parsedSources,
        out IReadOnlyCollection<string> invalidSources)
    {
        parsedSources = null;
        invalidSources = [];

        if (rawSources is null || rawSources.Count == 0)
            return true;

        List<DiscoverySourceKind> parsed = [];
        List<string> invalid = [];

        foreach (var source in rawSources)
        {
            if (source.ValueKind == JsonValueKind.String)
            {
                var value = source.GetString();
                if (!string.IsNullOrWhiteSpace(value) &&
                    Enum.TryParse<DiscoverySourceKind>(value, ignoreCase: true, out var enumValue))
                {
                    parsed.Add(enumValue);
                }
                else
                {
                    invalid.Add(value ?? "<null>");
                }

                continue;
            }

            if (source.ValueKind == JsonValueKind.Number && source.TryGetInt32(out var numericValue))
            {
                if (Enum.IsDefined(typeof(DiscoverySourceKind), numericValue))
                {
                    parsed.Add((DiscoverySourceKind)numericValue);
                }
                else
                {
                    invalid.Add(numericValue.ToString());
                }

                continue;
            }

            if (source.ValueKind == JsonValueKind.Null)
                continue;

            invalid.Add(source.ToString());
        }

        if (invalid.Count > 0)
        {
            invalidSources = invalid.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            return false;
        }

        parsedSources = parsed.Distinct().ToArray();
        return true;
    }

    public sealed record DiscoverResourcesRequest(
        string ExternalSubscriptionId,
        Guid ProjectId,
        string? OrganizationId,
        IReadOnlyCollection<JsonElement>? Sources);

    public sealed record DiscoverResourcesErrorResponse(
        string ErrorCode,
        string ErrorMessage,
        DiscoveryExecutionStatus Status,
        DiscoveryEscalationLevel EscalationLevel,
        string? UserActionHint);

    public sealed record DiscoverResourcesValidationErrorResponse(
        string ErrorCode,
        string ErrorMessage,
        string Field,
        IReadOnlyCollection<string> InvalidValues,
        IReadOnlyCollection<string> AllowedValues);
}
