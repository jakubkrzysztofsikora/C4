using C4.Modules.Visualization.Application.Ports;
using C4.Modules.Visualization.Application.SaveViewPreset;
using C4.Modules.Visualization.Domain.Preset;
using C4.Shared.Kernel;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class SaveViewPresetHandlerTests
{
    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithPresetDetails()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SaveViewPresetHandler(repository, unitOfWork, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new SaveViewPresetCommand(projectId, "My Preset", "{\"zoom\":1}"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ProjectId.Should().Be(projectId);
        result.Value.Name.Should().Be("My Preset");
    }

    [Fact]
    public async Task Handle_ValidCommand_PersistsPresetToRepository()
    {
        var projectId = Guid.NewGuid();
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SaveViewPresetHandler(repository, unitOfWork, new AlwaysAuthorizingService());

        await handler.Handle(
            new SaveViewPresetCommand(projectId, "Saved View", "{\"layout\":\"force\"}"),
            CancellationToken.None);

        repository.AddedPreset.Should().NotBeNull();
        repository.AddedPreset!.ProjectId.Should().Be(projectId);
        repository.AddedPreset.Name.Should().Be("Saved View");
        repository.AddedPreset.Json.Should().Be("{\"layout\":\"force\"}");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChanges()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SaveViewPresetHandler(repository, unitOfWork, new AlwaysAuthorizingService());

        await handler.Handle(
            new SaveViewPresetCommand(Guid.NewGuid(), "View", "{}"),
            CancellationToken.None);

        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsNewPresetId()
    {
        var repository = new FakeRepository();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SaveViewPresetHandler(repository, unitOfWork, new AlwaysAuthorizingService());

        var result = await handler.Handle(
            new SaveViewPresetCommand(Guid.NewGuid(), "View", "{}"),
            CancellationToken.None);

        result.Value.PresetId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var handler = new SaveViewPresetHandler(new FakeRepository(), new FakeUnitOfWork(), new DenyingAuthorizationService());

        var result = await handler.Handle(
            new SaveViewPresetCommand(Guid.NewGuid(), "View", "{}"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeRepository : IViewPresetRepository
    {
        public ViewPreset? AddedPreset { get; private set; }

        public Task AddAsync(ViewPreset preset, CancellationToken cancellationToken)
        {
            AddedPreset = preset;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<ViewPreset>> GetByProjectAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ViewPreset>>([]);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }
    }

    private sealed class AlwaysAuthorizingService : IProjectAuthorizationService
    {
        public Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Success(true));

        public Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Success(true));
    }

    private sealed class DenyingAuthorizationService : IProjectAuthorizationService
    {
        public Task<Result<bool>> AuthorizeAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Failure(new Error("authorization.denied", "Access denied.")));

        public Task<Result<bool>> AuthorizeOwnerAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult(Result<bool>.Failure(new Error("authorization.denied", "Access denied.")));
    }
}
