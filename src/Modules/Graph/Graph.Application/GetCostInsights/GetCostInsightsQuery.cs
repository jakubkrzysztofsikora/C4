using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetCostInsights;

public sealed record GetCostInsightsQuery(Guid ProjectId) : IRequest<Result<GetCostInsightsResponse>>;

public sealed record CostNodeDto(
    Guid NodeId,
    string Name,
    string ServiceType,
    double HourlyCostUsd,
    string Recommendation,
    string DataProvenance = "heuristic",
    bool IsHeuristic = true);

public sealed record GetCostInsightsResponse(
    Guid ProjectId,
    double TotalHourlyCostUsd,
    IReadOnlyCollection<CostNodeDto> TopCostNodes,
    IReadOnlyCollection<string> Recommendations,
    string DataProvenance = "heuristic",
    DateTime? GeneratedAtUtc = null,
    bool IsHeuristic = true);
