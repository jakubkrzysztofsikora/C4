using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Errors;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Visualization.Application.ExportDiagram;

public sealed class ExportDiagramHandler(IDiagramReadModel readModel, IEnumerable<IDiagramExporter> exporters)
    : IRequestHandler<ExportDiagramCommand, Result<ExportDiagramResponse>>
{
    public async Task<Result<ExportDiagramResponse>> Handle(ExportDiagramCommand request, CancellationToken cancellationToken)
    {
        var diagram = await readModel.GetDiagramJsonAsync(request.ProjectId, cancellationToken);
        if (diagram is null) return Result<ExportDiagramResponse>.Failure(VisualizationErrors.DiagramNotFound(request.ProjectId));

        var format = request.Format.ToLowerInvariant();
        var exporter = format switch
        {
            "svg" => exporters.FirstOrDefault(e => e.GetType().Name.Contains("Svg")),
            "pdf" => exporters.FirstOrDefault(e => e.GetType().Name.Contains("Pdf")),
            _ => null
        };

        if (exporter is null) return Result<ExportDiagramResponse>.Failure(VisualizationErrors.UnsupportedExportFormat(request.Format));

        var bytes = exporter.Export(diagram);
        var contentType = format == "svg" ? "image/svg+xml" : "application/pdf";
        return Result<ExportDiagramResponse>.Success(new ExportDiagramResponse(contentType, bytes));
    }
}
