using C4.Modules.Visualization.Domain.Preset;

namespace C4.Modules.Visualization.Application.Ports;

public interface IViewPresetRepository
{
    Task AddAsync(ViewPreset preset, CancellationToken cancellationToken);
    Task<IReadOnlyCollection<ViewPreset>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken);
}
