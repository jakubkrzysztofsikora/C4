namespace C4.Modules.Visualization.Domain.Diagram;

public sealed record DiagramView(Guid ProjectId, string Json, DateTime CreatedAtUtc);
