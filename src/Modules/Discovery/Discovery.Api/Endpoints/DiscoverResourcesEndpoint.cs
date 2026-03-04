using C4.Modules.Discovery.Application.DiscoverResources;
using C4.Modules.Discovery.Application.Ports;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;
using Microsoft.Extensions.Logging;

namespace C4.Modules.Discovery.Api.Endpoints;

public sealed class DiscoverResourcesEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/discovery/subscriptions/{subscriptionId:guid}/discover", async (Guid subscriptionId, DiscoverResourcesRequest request, ISender sender, ILogger<DiscoverResourcesEndpoint> logger, CancellationToken ct) =>
        {
            try
            {
                var result = await sender.Send(new DiscoverResourcesCommand(subscriptionId, request.ExternalSubscriptionId, request.ProjectId, request.OrganizationId, request.Sources), ct);

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

    public sealed record DiscoverResourcesRequest(string ExternalSubscriptionId, Guid ProjectId, string? OrganizationId, IReadOnlyCollection<DiscoverySourceKind>? Sources);

    public sealed record DiscoverResourcesErrorResponse(
        string ErrorCode,
        string ErrorMessage,
        DiscoveryExecutionStatus Status,
        DiscoveryEscalationLevel EscalationLevel,
        string? UserActionHint);
}
