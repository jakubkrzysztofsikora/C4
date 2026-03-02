using C4.Shared.Kernel.IntegrationEvents;
using C4.Modules.Telemetry.Application.Ports;
using C4.Modules.Telemetry.Application.SyncApplicationInsightsTelemetry;
using C4.Modules.Telemetry.Domain.Metrics;
using C4.Shared.Kernel;
using MediatR;

namespace C4.Modules.Telemetry.Tests.Application;

public sealed class SyncApplicationInsightsTelemetryHandlerTests
{
    [Fact]
    public async Task Handle_IngestsMetricsAndPublishesIntegrationEvent()
    {
        var client = new FakeAppInsightsClient();
        var repo = new FakeTelemetryRepository();
        var mediator = new FakeMediator();
        var unitOfWork = new FakeUnitOfWork();
        var handler = new SyncApplicationInsightsTelemetryHandler(client, repo, mediator, unitOfWork, new AlwaysAuthorizingService());

        var result = await handler.Handle(new SyncApplicationInsightsTelemetryCommand(Guid.NewGuid(), 15), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.MetricsIngested.Should().Be(2);
        repo.Metrics.Should().HaveCount(2);
        mediator.PublishedTelemetryEvents.Should().HaveCount(1);
        unitOfWork.SaveCalls.Should().Be(1);
    }

    [Fact]
    public async Task Handle_UnauthorizedProject_ReturnsFailure()
    {
        var handler = new SyncApplicationInsightsTelemetryHandler(
            new FakeAppInsightsClient(),
            new FakeTelemetryRepository(),
            new FakeMediator(),
            new FakeUnitOfWork(),
            new DenyingAuthorizationService());

        var result = await handler.Handle(new SyncApplicationInsightsTelemetryCommand(Guid.NewGuid(), 15), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("authorization.denied");
    }

    private sealed class FakeAppInsightsClient : IApplicationInsightsClient
    {
        public Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ApplicationInsightsHealthRecord>>([
                new ApplicationInsightsHealthRecord("api", 0.84, DateTime.UtcNow),
                new ApplicationInsightsHealthRecord("worker", 0.41, DateTime.UtcNow)
            ]);

        public Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(Guid projectId, TimeSpan lookbackWindow, CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyCollection<ApplicationInsightsDependencyRecord>>([]);
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
            => Task.FromResult<ServiceHealth?>(null);

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
