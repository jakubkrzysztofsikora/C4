using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetSecurityFindings;

public sealed class GetSecurityFindingsHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetSecurityFindingsQuery, Result<GetSecurityFindingsResponse>>
{
    public async Task<Result<GetSecurityFindingsResponse>> Handle(GetSecurityFindingsQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetSecurityFindingsResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetSecurityFindingsResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        List<SecurityFindingDto> findings = [];

        foreach (var node in graph.Nodes)
        {
            var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? string.Empty;
            var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);

            if (resolved.ServiceType.Equals("external", StringComparison.OrdinalIgnoreCase)
                && !resolved.IsInfrastructure)
            {
                findings.Add(new SecurityFindingDto(
                    node.Id.Value,
                    node.Name,
                    "medium",
                    "exposure",
                    "Externally exposed boundary component detected.",
                    "Validate network ACLs, WAF/rate limits, and authentication controls.",
                    "heuristic",
                    true));
            }

            if (resolved.ServiceType.Equals("storage", StringComparison.OrdinalIgnoreCase)
                || resolved.ServiceType.Equals("database", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new SecurityFindingDto(
                    node.Id.Value,
                    node.Name,
                    "high",
                    "data",
                    "Data-bearing service requires encryption and access review.",
                    "Enforce least privilege, key rotation, and audit logging for data access.",
                    "heuristic",
                    true));
            }

            if (resolved.ClassificationSource.Equals("fallback", StringComparison.OrdinalIgnoreCase))
            {
                findings.Add(new SecurityFindingDto(
                    node.Id.Value,
                    node.Name,
                    "low",
                    "visibility",
                    "Resource classification confidence is low.",
                    "Improve tagging and architecture metadata for more accurate security analysis.",
                    "heuristic",
                    true));
            }
        }

        var deduped = findings
            .GroupBy(f => (f.NodeId, f.Category, f.Message))
            .Select(g => g.First())
            .Take(200)
            .ToArray();

        return Result<GetSecurityFindingsResponse>.Success(
            new GetSecurityFindingsResponse(
                request.ProjectId,
                deduped.Length,
                deduped,
                DataProvenance: "heuristic",
                GeneratedAtUtc: DateTime.UtcNow,
                IsHeuristic: true));
    }

    private static string? ExtractResourceGroup(string resourceId)
    {
        var lower = resourceId.ToLowerInvariant();
        var rgIndex = lower.IndexOf("/resourcegroups/", StringComparison.Ordinal);
        if (rgIndex < 0) return null;

        var start = rgIndex + "/resourcegroups/".Length;
        var end = lower.IndexOf('/', start);
        if (end < 0) return lower[start..];

        return lower[start..end];
    }
}
