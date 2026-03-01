using C4.Modules.Visualization.Application.GetDiagram;
using C4.Modules.Visualization.Application.Ports;
using C4.Shared.Kernel;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class GetDiagramHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDiagram()
    {
        var handler = new GetDiagramHandler(new FakeReadModel(), new AlwaysAuthorizingService());
        var result = await handler.Handle(new GetDiagramQuery(Guid.NewGuid()), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var handler = new GetDiagramHandler(new FakeReadModel(), new DenyingAuthorizationService());

        var result = await handler.Handle(new GetDiagramQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeReadModel : IDiagramReadModel
    {
        public Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<string?>("{}");
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
