using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraph;

public sealed record GetGraphQuery(
    Guid ProjectId,
    string? Level = null,
    string? Scope = null,
    string? GroupBy = null,
    string? IncludeInfrastructure = null,
    string? Environment = null,
    Guid? SnapshotId = null) : IRequest<Result<GraphDto>>;
