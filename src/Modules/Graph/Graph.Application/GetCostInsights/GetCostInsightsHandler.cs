using C4.Modules.Graph.Application.GetGraph;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetCostInsights;

public sealed class GetCostInsightsHandler(
    IArchitectureGraphRepository repository,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetCostInsightsQuery, Result<GetCostInsightsResponse>>
{
    public async Task<Result<GetCostInsightsResponse>> Handle(GetCostInsightsQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetCostInsightsResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetCostInsightsResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var costs = graph.Nodes
            .Select(node =>
            {
                var resourceGroup = ExtractResourceGroup(node.ExternalResourceId) ?? string.Empty;
                var resolved = GraphClassificationResolver.Resolve(node, resourceGroup);
                var level = Enum.TryParse<C4.Modules.Graph.Domain.C4Level>(resolved.C4Level, true, out var parsed)
                    ? parsed
                    : C4.Modules.Graph.Domain.C4Level.Container;
                var hourly = EstimateHourlyCost(resolved.ServiceType, level, resolved.IsInfrastructure);
                return new CostNodeDto(
                    node.Id.Value,
                    node.Name,
                    resolved.ServiceType,
                    hourly,
                    BuildRecommendation(resolved.ServiceType, resolved.IsInfrastructure, hourly));
            })
            .OrderByDescending(n => n.HourlyCostUsd)
            .ToArray();

        var total = Math.Round(costs.Sum(c => c.HourlyCostUsd), 2);
        var topCostNodes = costs.Take(20).ToArray();

        var recommendations = costs
            .Where(c => c.Recommendation.Length > 0)
            .Select(c => c.Recommendation)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();

        return Result<GetCostInsightsResponse>.Success(new GetCostInsightsResponse(request.ProjectId, total, topCostNodes, recommendations));
    }

    private static string BuildRecommendation(string serviceType, bool isInfrastructure, double hourly)
    {
        if (isInfrastructure && hourly > 0.01)
            return "Review infrastructure helper resources and disable unnecessary always-on components.";
        return serviceType switch
        {
            "database" => "Review database sizing and retention policies to reduce spend.",
            "storage" => "Apply lifecycle/archival policies for lower storage tiers.",
            "monitoring" => "Tune log ingestion and retention to reduce observability costs.",
            "app" or "api" => "Scale down or schedule non-production compute outside business hours.",
            _ => ""
        };
    }

    private static double EstimateHourlyCost(string serviceType, C4.Modules.Graph.Domain.C4Level level, bool isInfrastructure)
    {
        if (isInfrastructure) return 0.02;

        var baseCost = serviceType switch
        {
            "database" => 1.25,
            "storage" => 0.32,
            "api" => 0.18,
            "app" => 0.24,
            "queue" => 0.10,
            "cache" => 0.46,
            "monitoring" => 0.12,
            _ => 0.08
        };

        return level switch
        {
            C4.Modules.Graph.Domain.C4Level.Context => Math.Round(baseCost * 0.5, 2),
            C4.Modules.Graph.Domain.C4Level.Component => Math.Round(baseCost * 0.75, 2),
            _ => Math.Round(baseCost, 2)
        };
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
