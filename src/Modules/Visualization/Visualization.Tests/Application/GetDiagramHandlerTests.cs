using C4.Modules.Visualization.Application.GetDiagram;
using C4.Modules.Visualization.Application.Ports;

namespace C4.Modules.Visualization.Tests.Application;

public sealed class GetDiagramHandlerTests
{
    [Fact]
    public async Task Handle_ReturnsDiagram()
    {
        var handler = new GetDiagramHandler(new FakeReadModel());
        var result = await handler.Handle(new GetDiagramQuery(Guid.NewGuid()), CancellationToken.None);
        result.IsSuccess.Should().BeTrue();
    }

    private sealed class FakeReadModel : IDiagramReadModel
    {
        public Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken) => Task.FromResult<string?>("{}");
    }
}
