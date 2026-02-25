using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.AnalyzeArchitecture;

public sealed class AnalyzeArchitectureHandler(IArchitectureGraphRepository repository, IArchitectureAnalyzer analyzer)
    : IRequestHandler<AnalyzeArchitectureCommand, Result<ArchitectureAnalysisResponse>>
{
    public async Task<Result<ArchitectureAnalysisResponse>> Handle(AnalyzeArchitectureCommand request, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ArchitectureAnalysisResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodesDescription = string.Join(", ", graph.Nodes.Select(n => $"{n.Name} ({n.Level})"));
        var edgesDescription = string.Join(", ", graph.Edges.Select(e => $"{e.SourceNodeId} -> {e.TargetNodeId}"));

        var analysis = await analyzer.AnalyzeAsync(nodesDescription, edgesDescription, cancellationToken);

        return Result<ArchitectureAnalysisResponse>.Success(
            new ArchitectureAnalysisResponse(request.ProjectId, analysis.Summary, analysis.Recommendations));
    }
}
