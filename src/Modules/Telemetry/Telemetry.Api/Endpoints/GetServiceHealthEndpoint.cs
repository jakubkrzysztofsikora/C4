using C4.Modules.Telemetry.Application.GetServiceHealth;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Telemetry.Api.Endpoints;

public sealed class GetServiceHealthEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/telemetry/{service}/health", async (Guid projectId, string service, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new GetServiceHealthQuery(projectId, service), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });
    }
}
