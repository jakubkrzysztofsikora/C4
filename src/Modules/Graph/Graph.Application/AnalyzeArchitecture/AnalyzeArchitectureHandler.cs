using System.Text.Json;
using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.AnalyzeArchitecture;

public sealed class AnalyzeArchitectureHandler(
    IArchitectureGraphRepository repository,
    IArchitectureAnalyzer analyzer,
    IProjectAuthorizationService authorizationService
) : IRequestHandler<AnalyzeArchitectureCommand, Result<ArchitectureAnalysisResponse>>
{
    private static readonly int MaxNodeNameLength = 200;

    public async Task<Result<ArchitectureAnalysisResponse>> Handle(AnalyzeArchitectureCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ArchitectureAnalysisResponse>.Failure(authCheck.Error);

        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ArchitectureAnalysisResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var sanitizedNodes = graph.Nodes.Select(n => new { Name = SanitizeName(n.Name), n.Level });
        var sanitizedEdges = graph.Edges.Select(e => new { e.SourceNodeId, e.TargetNodeId });

        var nodesDescription = JsonSerializer.Serialize(sanitizedNodes);
        var edgesDescription = JsonSerializer.Serialize(sanitizedEdges);

        var analysis = await analyzer.AnalyzeAsync(request.ProjectId, nodesDescription, edgesDescription, cancellationToken);

        return Result<ArchitectureAnalysisResponse>.Success(
            new ArchitectureAnalysisResponse(request.ProjectId, analysis.Summary, analysis.Recommendations));
    }

    private static string SanitizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return string.Empty;

        return name.Length > MaxNodeNameLength ? name[..MaxNodeNameLength] : name;
    }
}
