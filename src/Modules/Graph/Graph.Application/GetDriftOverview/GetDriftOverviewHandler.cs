using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using C4.Shared.Kernel.Contracts;
using MediatR;

namespace C4.Modules.Graph.Application.GetDriftOverview;

public sealed class GetDriftOverviewHandler(
    IArchitectureGraphRepository repository,
    IDriftQueryService driftQueryService,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<GetDriftOverviewQuery, Result<GetDriftOverviewResponse>>
{
    public async Task<Result<GetDriftOverviewResponse>> Handle(GetDriftOverviewQuery request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<GetDriftOverviewResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdReadOnlyAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<GetDriftOverviewResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var resources = graph.Nodes.Select(n => n.ExternalResourceId).ToArray();
        var drifted = new HashSet<string>(await driftQueryService.GetDriftedResourceIdsAsync(resources, cancellationToken), StringComparer.OrdinalIgnoreCase);

        var nodes = graph.Nodes
            .Where(n => drifted.Contains(n.ExternalResourceId))
            .Select(n => new DriftedNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString()))
            .ToArray();

        return Result<GetDriftOverviewResponse>.Success(new GetDriftOverviewResponse(request.ProjectId, nodes.Length, nodes));
    }
}
