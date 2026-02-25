using C4.Modules.Graph.Application.Ports;
using C4.Modules.Graph.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.GetThreatAssessment;

public sealed class GetThreatAssessmentHandler(IArchitectureGraphRepository repository, IThreatDetector threatDetector)
    : IRequestHandler<GetThreatAssessmentQuery, Result<ThreatAssessmentResponse>>
{
    public async Task<Result<ThreatAssessmentResponse>> Handle(GetThreatAssessmentQuery request, CancellationToken cancellationToken)
    {
        var graph = await repository.GetByProjectIdAsync(request.ProjectId, cancellationToken);
        if (graph is null) return Result<ThreatAssessmentResponse>.Failure(GraphErrors.GraphNotFound(request.ProjectId));

        var nodesDescription = string.Join(", ", graph.Nodes.Select(n => $"{n.Name} ({n.Level})"));
        var edgesDescription = string.Join(", ", graph.Edges.Select(e => $"{e.SourceNodeId} -> {e.TargetNodeId}"));

        var result = await threatDetector.DetectThreatsAsync(nodesDescription, edgesDescription, cancellationToken);

        return Result<ThreatAssessmentResponse>.Success(
            new ThreatAssessmentResponse(request.ProjectId, result.RiskLevel, result.Threats));
    }
}
