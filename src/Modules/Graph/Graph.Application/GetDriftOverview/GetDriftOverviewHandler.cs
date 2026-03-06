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
        Task<IReadOnlyCollection<string>> driftedTask = driftQueryService.GetDriftedResourceIdsAsync(resources, cancellationToken);
        Task<DriftRunRecord?> runTask = GetLatestRunSafelyAsync(request.ProjectId, cancellationToken);
        await Task.WhenAll(driftedTask, runTask);

        var drifted = new HashSet<string>(driftedTask.Result, StringComparer.OrdinalIgnoreCase);
        DriftRunRecord? latestRun = runTask.Result;

        var nodes = graph.Nodes
            .Where(n => drifted.Contains(n.ExternalResourceId))
            .Select(n => new DriftedNodeDto(n.Id.Value, n.Name, n.ExternalResourceId, n.Level.ToString()))
            .ToArray();

        string status = latestRun?.Status switch
        {
            DriftRunStatus.NotRun => "not-run",
            DriftRunStatus.Running => "running",
            DriftRunStatus.Completed => "completed",
            DriftRunStatus.Failed => "failed",
            null => "not-run",
            _ => "not-run"
        };

        return Result<GetDriftOverviewResponse>.Success(
            new GetDriftOverviewResponse(
                request.ProjectId,
                nodes.Length,
                nodes,
                latestRun?.LastRunAtUtc,
                status,
                latestRun?.Error));
    }

    private async Task<DriftRunRecord?> GetLatestRunSafelyAsync(Guid projectId, CancellationToken cancellationToken)
    {
        try
        {
            return await driftQueryService.GetLatestRunAsync(projectId, cancellationToken);
        }
        catch
        {
            return null;
        }
    }
}
