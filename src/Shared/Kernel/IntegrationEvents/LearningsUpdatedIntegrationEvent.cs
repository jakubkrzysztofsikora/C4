using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record LearningsUpdatedIntegrationEvent(Guid ProjectId, int InsightsCount) : INotification;
