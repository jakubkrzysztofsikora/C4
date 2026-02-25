using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphDiff;

public sealed record GetGraphDiffQuery(Guid ProjectId, Guid FromSnapshotId, Guid ToSnapshotId) : IRequest<Result<GetGraphDiffResponse>>;
public sealed record GetGraphDiffResponse(IReadOnlyCollection<string> AddedNodes, IReadOnlyCollection<string> RemovedNodes);
