using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using C4.Shared.Kernel.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Application.GetGraphSnapshots;

public sealed class GetGraphSnapshotsHandler(
    IArchitectureGraphRepository repository,
    [FromKeyedServices("Graph")] IUnitOfWork unitOfWork,
    IProjectAuthorizationService authorizationService,
    IMediator? mediator = null)
    : IRequestHandler<GetGraphSnapshotsQuery, Result<GetGraphSnapshotsResponse>>
{
    private static readonly TimeSpan BackfillTimeout = TimeSpan.FromSeconds(8);

    public async Task<Result<GetGraphSnapshotsResponse>> Handle(GetGraphSnapshotsQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetGraphSnapshotsResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetGraphSnapshotsResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var snapshots = graph.Snapshots
            .Where(s => !IsEmptySnapshot(s))
            .OrderBy(s => s.CreatedAtUtc)
            .Select(s => new GraphSnapshotDto(s.Id.Value, s.CreatedAtUtc, s.Source))
            .ToArray();

        if (snapshots.Length == 0 && graph.Nodes.Count > 0)
        {
            var backfilled = await TryBackfillSnapshotsAsync(request.ProjectId, cancellationToken);
            if (backfilled is not null)
                snapshots = backfilled;
        }

        return Result<GetGraphSnapshotsResponse>.Success(new GetGraphSnapshotsResponse(request.ProjectId, snapshots));
    }

    private async Task<GraphSnapshotDto[]?> TryBackfillSnapshotsAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(BackfillTimeout);

            var writableGraph = await repository.GetByProjectIdAsync(projectId, timeoutCts.Token);
            if (writableGraph is null || writableGraph.Nodes.Count == 0)
                return null;

            if (writableGraph.Snapshots.All(IsEmptySnapshot))
            {
                writableGraph.CreateSnapshot("auto-backfill");
                await repository.UpsertAsync(writableGraph, timeoutCts.Token);
                await unitOfWork.SaveChangesAsync(timeoutCts.Token);
                if (mediator is not null)
                {
                    await mediator.Publish(
                        new GraphChangedIntegrationEvent(projectId, "snapshot-backfill", DateTime.UtcNow),
                        timeoutCts.Token);
                }
            }

            return writableGraph.Snapshots
                .Where(s => !IsEmptySnapshot(s))
                .OrderBy(s => s.CreatedAtUtc)
                .Select(s => new GraphSnapshotDto(s.Id.Value, s.CreatedAtUtc, s.Source))
                .ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex)
        {
            _ = ex;
            return null;
        }
    }

    private static bool IsEmptySnapshot(Domain.GraphSnapshot.GraphSnapshot snapshot)
        => snapshot.Nodes.Count == 0 && snapshot.Edges.Count == 0;
}
