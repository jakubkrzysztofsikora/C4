using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetClassificationGap;

public sealed class GetClassificationGapHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetClassificationGapQuery, Result<ClassificationGapResponse>>
{
    public async Task<Result<ClassificationGapResponse>> Handle(GetClassificationGapQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ClassificationGapResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ClassificationGapResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodes = graph.Nodes.ToArray();
        var comparisons = nodes.Select(node =>
        {
            var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? "";
            var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);
            var currentServiceType = string.IsNullOrWhiteSpace(node.Properties.Technology) ? "external" : node.Properties.Technology;
            return new NodeComparison(
                node.Name,
                node.ExternalResourceId,
                resolved.ArmType,
                node.Level.ToString(),
                resolved.C4Level,
                currentServiceType,
                resolved.ServiceType);
        }).ToArray();

        int mismatchCount = comparisons.Count(c =>
            !c.CurrentLevel.Equals(c.EffectiveLevel, StringComparison.OrdinalIgnoreCase)
            || !c.CurrentServiceType.Equals(c.EffectiveServiceType, StringComparison.OrdinalIgnoreCase));

        var response = new ClassificationGapResponse(
            comparisons.Length,
            mismatchCount,
            comparisons.GroupBy(c => c.CurrentLevel, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new MetricCount(g.Key, g.Count()))
                .ToArray(),
            comparisons.GroupBy(c => c.EffectiveLevel, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new MetricCount(g.Key, g.Count()))
                .ToArray(),
            comparisons.GroupBy(c => c.CurrentServiceType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new MetricCount(g.Key, g.Count()))
                .ToArray(),
            comparisons.GroupBy(c => c.EffectiveServiceType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Select(g => new MetricCount(g.Key, g.Count()))
                .ToArray(),
            comparisons
                .Where(c => !c.CurrentLevel.Equals(c.EffectiveLevel, StringComparison.OrdinalIgnoreCase)
                            || !c.CurrentServiceType.Equals(c.EffectiveServiceType, StringComparison.OrdinalIgnoreCase))
                .GroupBy(c => c.ArmType, StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(g => g.Count())
                .Take(20)
                .Select(g => new TypeMismatchCount(g.Key, g.Count()))
                .ToArray(),
            comparisons
                .Where(c => !c.CurrentLevel.Equals(c.EffectiveLevel, StringComparison.OrdinalIgnoreCase)
                            || !c.CurrentServiceType.Equals(c.EffectiveServiceType, StringComparison.OrdinalIgnoreCase))
                .Take(30)
                .Select(c => new MismatchSample(c.Name, c.ExternalResourceId, c.ArmType, c.CurrentLevel, c.EffectiveLevel, c.CurrentServiceType, c.EffectiveServiceType))
                .ToArray());

        return Result<ClassificationGapResponse>.Success(response);
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

    private sealed record NodeComparison(
        string Name,
        string ExternalResourceId,
        string ArmType,
        string CurrentLevel,
        string EffectiveLevel,
        string CurrentServiceType,
        string EffectiveServiceType);
}
