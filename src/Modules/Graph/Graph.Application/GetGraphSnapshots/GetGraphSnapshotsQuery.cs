using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetGraphSnapshots;

public sealed record GetGraphSnapshotsQuery(Guid ProjectId) : IRequest<Result<GetGraphSnapshotsResponse>>;

public sealed record GraphSnapshotDto(Guid SnapshotId, DateTime CreatedAtUtc, string Source);

public sealed record GetGraphSnapshotsResponse(Guid ProjectId, IReadOnlyCollection<GraphSnapshotDto> Snapshots);
