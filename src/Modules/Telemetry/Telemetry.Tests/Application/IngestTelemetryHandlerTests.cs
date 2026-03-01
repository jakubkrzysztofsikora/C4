using C4.Modules.Telemetry.Application.IngestTelemetry;
using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Tests.Application;

public sealed class IngestTelemetryHandlerTests
{
    [Fact]
    public async Task Handle_PublishesTelemetryUpdatedEvent()
    {
        var repo = new FakeTelemetryRepository();
        var mediator = new FakeMediator();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new IngestTelemetryHandler(repo, mediator, unitOfWork, new AlwaysAuthorizingService());

        var result = await handler.Handle(new IngestTelemetryCommand(Guid.NewGuid(), "api", 0.92), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        mediator.PublishedTelemetryEvents.Should().HaveCount(1);
        mediator.PublishedTelemetryEvents[0].Services.Should().ContainSingle(s => s.Service == "api" && s.Status == "Green");
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var handler = new IngestTelemetryHandler(new FakeTelemetryRepository(), new FakeMediator(), new FakeUnitOfWork(), new DenyingAuthorizationService());

        var result = await handler.Handle(new IngestTelemetryCommand(Guid.NewGuid(), "api", 0.92), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeTelemetryRepository : ITelemetryRepository
    {
        public readonly List<MetricDataPoint> Metrics = [];

        public Task AddMetricAsync(MetricDataPoint metric, CancellationToken cancellationToken)
        {
            Metrics.Add(metric);
            return Task.CompletedTask;
        }

        public Task<ServiceHealth?> GetServiceHealthAsync(Guid projectId, string service, CancellationToken cancellationToken)
            => Task.FromResult<ServiceHealth?>(new ServiceHealth(projectId, service, 0.92, ServiceHealthStatus.Green, DateTime.UtcNow));

        public Task<IReadOnlyCollection<ServiceHealth>> GetAllServiceHealthAsync(Guid projectId, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ServiceHealth>>([]);
    }

    private sealed class FakeMediator : IMediator
    {
        public readonly List<TelemetryUpdatedIntegrationEvent> PublishedTelemetryEvents = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            if (notification is TelemetryUpdatedIntegrationEvent telemetryEvent)
            {
                PublishedTelemetryEvents.Add(telemetryEvent);
            }

            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        {
            if (notification is TelemetryUpdatedIntegrationEvent telemetryEvent)
            {
                PublishedTelemetryEvents.Add(telemetryEvent);
            }

            return Task.CompletedTask;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<TResponse> Send<TRequest, TResponse>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest<TResponse> => throw new NotImplementedException();
        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest => throw new NotImplementedException();
        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
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
