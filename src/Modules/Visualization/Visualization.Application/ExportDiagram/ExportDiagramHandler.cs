using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.ExportDiagram;

public sealed class ExportDiagramHandler(
    IDiagramReadModel readModel,
    IEnumerable<IDiagramExporter> exporters,
    IProjectAuthorizationService authorizationService)
    : IRequestHandler<ExportDiagramCommand, Result<ExportDiagramResponse>>
{
    public async Task<Result<ExportDiagramResponse>> Handle(ExportDiagramCommand request, CancellationToken cancellationToken)
    {
        var authCheck = await authorizationService.AuthorizeAsync(request.ProjectId, cancellationToken);
        if (!authCheck.IsSuccess) return Result<ExportDiagramResponse>.Failure(authCheck.Error);

        string? diagram = await readModel.GetDiagramJsonAsync(request.ProjectId, cancellationToken);
        if (diagram is null) return Result<ExportDiagramResponse>.Failure(VisualizationErrors.DiagramNotFound(request.ProjectId));

        string format = request.Format.ToLowerInvariant();
        IDiagramExporter? exporter = exporters.FirstOrDefault(e => e.Format == format);

        if (exporter is null) return Result<ExportDiagramResponse>.Failure(VisualizationErrors.UnsupportedExportFormat(request.Format));

        byte[] bytes = await exporter.ExportAsync(diagram, cancellationToken);
        string contentType = format == "svg" ? "image/svg+xml" : "application/pdf";
        return Result<ExportDiagramResponse>.Success(new ExportDiagramResponse(contentType, bytes));
    }
}
