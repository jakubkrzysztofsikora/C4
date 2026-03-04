using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphQuality;

public sealed class GetGraphQualityHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetGraphQualityQuery, Result<GetGraphQualityResponse>>
{
    public async Task<Result<GetGraphQualityResponse>> Handle(GetGraphQualityQuery request, CancellationToken cancellationToken)
    {
        var auth = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!auth.IsSuccess) return Result<GetGraphQualityResponse>.Failure(auth.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetGraphQualityResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var projections = graph.Nodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? string.Empty;
                var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);
                var environment = EnvironmentClassifier.InferEnvironment(node.Name, resourceGroup);
                return new
                {
                    Node = node,
                    Resolved = resolved,
                    Environment = environment
                };
            })
            .ToArray();

        return Result<GetGraphQualityResponse>.Success(new GetGraphQualityResponse(
            request.ProjectId,
            projections.Length,
            projections.Count(item => item.Resolved.ClassificationSource.Equals("fallback", StringComparison.OrdinalIgnoreCase)),
            projections.Count(item => item.Environment.Equals("unknown", StringComparison.OrdinalIgnoreCase)),
            projections.Count(item => IsNonRuntimeNode(item.Node.ExternalResourceId)),
            projections.Count(item => LooksLikeRawIacDeclarationLabel(item.Node.Name)),
            DateTime.UtcNow));
    }

    private static bool IsNonRuntimeNode(string externalResourceId)
        => externalResourceId.StartsWith("/providers/", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeRawIacDeclarationLabel(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        var normalized = name.TrimStart();
        return normalized.StartsWith("resource ", StringComparison.OrdinalIgnoreCase)
               || normalized.StartsWith("module ", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("Microsoft.", StringComparison.OrdinalIgnoreCase) && normalized.Contains("'", StringComparison.Ordinal);
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
