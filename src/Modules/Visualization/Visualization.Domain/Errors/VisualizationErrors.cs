using C4.Shared.Kernel;

namespace C4.Modules.Visualization.Domain.Errors;

public static class VisualizationErrors
{
    public static Error DiagramNotFound(Guid projectId) => new("visualization.diagram.not_found", $"Diagram for {projectId} not found.");
    public static Error UnsupportedExportFormat(string format) => new("visualization.export.unsupported", $"Format '{format}' is unsupported.");
}
