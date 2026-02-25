using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.GetDiagram;

public sealed record GetDiagramQuery(Guid ProjectId) : IRequest<Result<GetDiagramResponse>>;
public sealed record GetDiagramResponse(Guid ProjectId, string DiagramJson);
