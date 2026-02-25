namespace C4.Modules.Visualization.Domain.Preset;

public sealed record ViewPreset(Guid Id, Guid ProjectId, string Name, string Json, DateTime CreatedAtUtc)
{
    public static ViewPreset Create(Guid projectId, string name, string json) => new(Guid.NewGuid(), projectId, name.Trim(), json, DateTime.UtcNow);
}
