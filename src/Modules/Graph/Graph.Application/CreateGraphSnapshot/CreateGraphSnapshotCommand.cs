using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace C4.Modules.Graph.Application.CreateGraphSnapshot;

public sealed record CreateGraphSnapshotCommand(Guid ProjectId, string? Source)
    : IRequest<Result<GraphSnapshotDto>>;

public sealed record GraphSnapshotDto(Guid SnapshotId, DateTime CreatedAtUtc, string Source);

public sealed class CreateGraphSnapshotHandler(
    IArchitectureGraphRepository repository,
    [FromKeyedServices("Graph")] IUnitOfWork unitOfWork,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<CreateGraphSnapshotCommand, Result<GraphSnapshotDto>>
{
    public async Task<Result<GraphSnapshotDto>> Handle(CreateGraphSnapshotCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GraphSnapshotDto>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GraphSnapshotDto>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source.Trim();
        var snapshot = graph.CreateSnapshot(source);

        await repository.UpsertAsync(graph, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result<GraphSnapshotDto>.Success(new GraphSnapshotDto(snapshot.Id.Value, snapshot.CreatedAtUtc, snapshot.Source));
    }
}
