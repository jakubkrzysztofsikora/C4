using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Graph.Application.AnalyzeArchitecture;

public sealed record AnalyzeArchitectureCommand(Guid ProjectId) : IRequest<Result<ArchitectureAnalysisResponse>>;
public sealed record ArchitectureAnalysisResponse(Guid ProjectId, string Summary, IReadOnlyCollection<string> Recommendations);
