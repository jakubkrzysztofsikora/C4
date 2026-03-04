using C4.Modules.Visualization.Application.ExportDiagram;
using C4.Modules.Graph.Application.GetGraph;
using C4.Shared.Infrastructure.Endpoints;
using MediatR;
using System.Text.Json;

namespace C4.Modules.Visualization.Api.Endpoints;

public sealed class ExportDiagramEndpoint : IEndpoint
{
    public void MapEndpoint(IEndpointRouteBuilder app)
    {
        app.MapGet("/api/projects/{projectId:guid}/diagram/export", async (
            Guid projectId,
            string format,
            string? level,
            string? environment,
            string? scope,
            string? groupBy,
            string? includeInfrastructure,
            Guid? snapshotId,
            bool? hideOrphans,
            string? serviceType,
            string? technology,
            string? domain,
            string? risk,
            string? tag,
            string? search,
            bool? driftOnly,
            string? overlay,
            ISender sender,
            CancellationToken ct) =>
        {
            var graphResult = await sender.Send(
                new GetGraphQuery(projectId, level, scope, groupBy, includeInfrastructure, environment, snapshotId),
                ct);
            if (!graphResult.IsSuccess)
                return Results.NotFound(graphResult.Error);

            var nodes = graphResult.Value.Nodes.AsEnumerable();
            if (!IsAllFilter(serviceType))
                nodes = nodes.Where(n => n.ServiceType.Equals(serviceType!, StringComparison.OrdinalIgnoreCase));
            if (!IsAllFilter(technology))
                nodes = nodes.Where(n => n.Technology.Equals(technology!, StringComparison.OrdinalIgnoreCase));
            if (!IsAllFilter(domain))
                nodes = nodes.Where(n => n.Domain.Equals(domain!, StringComparison.OrdinalIgnoreCase));
            if (!IsAllFilter(risk))
                nodes = nodes.Where(n => string.Equals(n.RiskLevel, risk, StringComparison.OrdinalIgnoreCase));
            if (driftOnly is true)
                nodes = nodes.Where(n => n.Drift);

            var normalizedSearch = search?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedSearch))
            {
                var lowerSearch = normalizedSearch.ToLowerInvariant();
                nodes = nodes.Where(n =>
                    n.Name.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                    || n.ExternalResourceId.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase)
                    || n.Domain.Contains(lowerSearch, StringComparison.OrdinalIgnoreCase));
            }

            var normalizedTag = tag?.Trim();
            if (!string.IsNullOrWhiteSpace(normalizedTag))
            {
                var lowerTag = normalizedTag.ToLowerInvariant();
                nodes = nodes.Where(n =>
                    (n.Tags?.Any(t => t.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)) ?? false)
                    || n.Name.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)
                    || n.ExternalResourceId.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)
                    || n.Domain.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)
                    || n.ResourceGroup.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)
                    || n.ClassificationSource.Contains(lowerTag, StringComparison.OrdinalIgnoreCase)
                    || n.GroupKey.Contains(lowerTag, StringComparison.OrdinalIgnoreCase));
            }

            var filteredNodes = nodes.ToArray();
            var visibleNodeIds = filteredNodes.Select(n => n.Id).ToHashSet();
            var filteredEdges = graphResult.Value.Edges
                .Where(e => visibleNodeIds.Contains(e.SourceNodeId) && visibleNodeIds.Contains(e.TargetNodeId))
                .ToArray();

            if (hideOrphans is true)
            {
                var connectedNodeIds = filteredEdges
                    .SelectMany(e => new[] { e.SourceNodeId, e.TargetNodeId })
                    .ToHashSet();
                filteredNodes = filteredNodes
                    .Where(n => connectedNodeIds.Contains(n.Id))
                    .ToArray();
                visibleNodeIds = filteredNodes.Select(n => n.Id).ToHashSet();
                filteredEdges = filteredEdges
                    .Where(e => visibleNodeIds.Contains(e.SourceNodeId) && visibleNodeIds.Contains(e.TargetNodeId))
                    .ToArray();
            }

            var diagramJson = JsonSerializer.Serialize(new
            {
                overlay,
                nodes = filteredNodes.Select(node => new
                {
                    id = node.Id,
                    name = node.Name,
                    level = node.Level
                }),
                edges = filteredEdges.Select(edge => new
                {
                    id = edge.Id,
                    sourceNodeId = edge.SourceNodeId,
                    targetNodeId = edge.TargetNodeId
                })
            });

            var result = await sender.Send(new ExportDiagramCommand(projectId, format, diagramJson), ct);
            return result.IsSuccess ? Results.File(result.Value.Content, result.Value.ContentType) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();

        app.MapPost("/api/projects/{projectId:guid}/diagram/export", async (
            Guid projectId,
            string format,
            ExportDiagramRequest request,
            ISender sender,
            CancellationToken ct) =>
        {
            var result = await sender.Send(new ExportDiagramCommand(projectId, format, request.DiagramJson), ct);
            return result.IsSuccess ? Results.File(result.Value.Content, result.Value.ContentType) : Results.BadRequest(result.Error);
        })
        .RequireAuthorization();
    }

    public sealed record ExportDiagramRequest(string? DiagramJson);

    private static bool IsAllFilter(string? value)
        => string.IsNullOrWhiteSpace(value) || value.Equals("all", StringComparison.OrdinalIgnoreCase);
}
