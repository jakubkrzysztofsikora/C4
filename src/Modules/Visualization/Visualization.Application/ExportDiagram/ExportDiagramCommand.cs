using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.ExportDiagram;

public sealed record ExportDiagramCommand(Guid ProjectId, string Format) : IRequest<Result<ExportDiagramResponse>>;
public sealed record ExportDiagramResponse(string ContentType, byte[] Content);
