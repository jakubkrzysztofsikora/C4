using C4.Modules.Visualization.Application.GetViewPresets;
using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Domain.Preset;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class GetViewPresetsHandlerTests
{
    [Fact]
    public async Task Handle_ProjectWithPresets_ReturnsAllPresets()
    {
        var projectId = Guid.NewGuid();
        var presets = new[]
        {
            ViewPreset.Create(projectId, "Default View", "{\"zoom\":1}"),
            ViewPreset.Create(projectId, "Security View", "{\"zoom\":2}")
        };
        var repository = new FakeRepository(presets);
        var handler = new GetViewPresetsHandler(repository);

        var result = await handler.Handle(new GetViewPresetsQuery(projectId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Presets.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ProjectWithNoPresets_ReturnsEmptyCollection()
    {
        var repository = new FakeRepository([]);
        var handler = new GetViewPresetsHandler(repository);

        var result = await handler.Handle(new GetViewPresetsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Presets.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ProjectWithPresets_MapsPresetFieldsCorrectly()
    {
        var projectId = Guid.NewGuid();
        var preset = ViewPreset.Create(projectId, "Overview", "{\"layout\":\"tree\"}");
        var repository = new FakeRepository([preset]);
        var handler = new GetViewPresetsHandler(repository);

        var result = await handler.Handle(new GetViewPresetsQuery(projectId), CancellationToken.None);

        var dto = result.Value.Presets.Single();
        dto.Id.Should().Be(preset.Id);
        dto.Name.Should().Be("Overview");
        dto.Json.Should().Be("{\"layout\":\"tree\"}");
    }

    private sealed class FakeRepository(IReadOnlyCollection<ViewPreset> presets) : IViewPresetRepository
    {
        public Task AddAsync(ViewPreset preset, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<IReadOnlyCollection<ViewPreset>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(presets);
    }
}
