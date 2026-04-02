using System.Security.Cryptography;
using System.Text;
using C4.Modules.Graph.Application.GetGraph;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;

namespace C4.Modules.Graph.Api.Endpoints;

internal sealed class GetGraphEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet(
            "/api/projects/{projectId:guid}/graph",
            async (
                Guid projectId,
                string? level,
                string? scope,
                string? groupBy,
                string? includeInfrastructure,
                string? environment,
                Guid? snapshotId,
                HttpContext httpContext,
                ISender sender,
                CancellationToken ct) =>
        {
            var result = await sender.Send(
                new GetGraphQuery(projectId, level, scope, groupBy, includeInfrastructure, environment, snapshotId),
                ct);

            if (!result.IsSuccess)
                return Results.NotFound(result.Error);

            var etag = ComputeETag(result.Value);

            if (httpContext.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch)
                && ifNoneMatch.ToString() == etag)
            {
                return Results.StatusCode(StatusCodes.Status304NotModified);
            }

            httpContext.Response.Headers.ETag = etag;
            return Results.Ok(result.Value);
        })
        .RequireAuthorization();
    }

    private static string ComputeETag(GraphDto graph)
    {
        var raw = new StringBuilder();
        raw.Append(graph.ProjectId);
        raw.Append('|');
        raw.Append(graph.Nodes.Count);
        raw.Append('|');
        raw.Append(graph.Edges.Count);
        raw.Append('|');

        foreach (var node in graph.Nodes.OrderBy(n => n.Id))
        {
            raw.Append(node.Id);
            raw.Append(':');
            raw.Append(node.Health);
            raw.Append(':');
            raw.Append(node.Drift ? '1' : '0');
            raw.Append(';');
        }

        raw.Append('|');

        foreach (var edge in graph.Edges.OrderBy(e => e.Id))
        {
            raw.Append(edge.Id);
            raw.Append(':');
            raw.Append(edge.TrafficState);
            raw.Append(';');
        }

        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw.ToString()));
        return $"\"{Convert.ToHexString(hashBytes)[..32].ToLowerInvariant()}\"";
    }
}
