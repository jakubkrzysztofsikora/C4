using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Preset;
using Microsoft.EntityFrameworkCore;

namespace C4.Modules.Visualization.Infrastructure.Persistence.Repositories;

public sealed class ViewPresetRepository(VisualizationDbContext dbContext) : IViewPresetRepository
{
    public async Task AddAsync(ViewPreset preset, CancellationToken cancellationToken) =>
        await dbContext.ViewPresets.AddAsync(preset, cancellationToken);

    public async Task<IReadOnlyCollection<ViewPreset>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken) =>
        await dbContext.ViewPresets
            .Where(p => p.ProjectId == projectId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(cancellationToken);
}
