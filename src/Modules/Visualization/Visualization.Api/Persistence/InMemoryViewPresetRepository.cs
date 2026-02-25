using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Preset;

namespace C4.Modules.Visualization.Api.Persistence;

public sealed class InMemoryViewPresetRepository : IViewPresetRepository
{
    private readonly List<ViewPreset> _presets = [];

    public Task AddAsync(ViewPreset preset, CancellationToken cancellationToken)
    {
        _presets.Add(preset);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<ViewPreset>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken)
        => Task.FromResult<IReadOnlyCollection<ViewPreset>>(_presets.Where(p => p.ProjectId == projectId).ToArray());
}
