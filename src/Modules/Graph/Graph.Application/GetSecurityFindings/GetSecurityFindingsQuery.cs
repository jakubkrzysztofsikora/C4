using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetSecurityFindings;

public sealed record GetSecurityFindingsQuery(Guid ProjectId) : IRequest<Result<GetSecurityFindingsResponse>>;

public sealed record SecurityFindingDto(
    Guid NodeId,
    string NodeName,
    string Severity,
    string Category,
    string Message,
    string Recommendation,
    string DataProvenance = "heuristic",
    bool IsHeuristic = true);

public sealed record GetSecurityFindingsResponse(
    Guid ProjectId,
    int TotalFindings,
    IReadOnlyCollection<SecurityFindingDto> Findings,
    string DataProvenance = "heuristic",
    DateTime? GeneratedAtUtc = null,
    bool IsHeuristic = true);
