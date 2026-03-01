using C4.Modules.Telemetry.Application.GetServiceHealth;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;

namespace C4.Modules.Telemetry.Tests.Application;

public sealed class GetServiceHealthHandlerTests
{
    [Fact]
    public async Task Handle_ExistingHealth_ReturnsValue()
    {
        var repo = new FakeRepository();
        var handler = new GetServiceHealthHandler(repo, new AlwaysAuthorizingService());

        var result = await handler.Handle(new GetServiceHealthQuery(Guid.Parse("11111111-1111-1111-1111-111111111111"), "api"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Green");
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var handler = new GetServiceHealthHandler(new FakeRepository(), new DenyingAuthorizationService());

        var result = await handler.Handle(new GetServiceHealthQuery(Guid.NewGuid(), "api"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeRepository : ITelemetryRepository
    {
        public Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
            => Task.FromResult<ServiceHealth?>(new ServiceHealth(projectId, service, .9, ServiceHealthStatus.Green, DateTime.UtcNow));

        public Task<IReadOnlyCollection<ServiceHealth>> GetAllServiceHealthAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ServiceHealth>>([]);
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
