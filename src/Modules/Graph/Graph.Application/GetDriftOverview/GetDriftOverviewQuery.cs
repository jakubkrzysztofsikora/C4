using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetDriftOverview;

public sealed record GetDriftOverviewQuery(Guid ProjectId) : IRequest<Result<GetDriftOverviewResponse>>;

public sealed record DriftedNodeDto(Guid NodeId, string Name, string ExternalResourceId, string Level);

public sealed record GetDriftOverviewResponse(
    Guid ProjectId,
    int DriftedCount,
    IReadOnlyCollection<DriftedNodeDto> DriftedNodes,
    DateTime? LastRunAtUtc = null,
    string Status = "not-run",
    string? Error = null);
