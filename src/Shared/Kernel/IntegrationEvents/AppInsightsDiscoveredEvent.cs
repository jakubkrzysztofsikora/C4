using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record AppInsightsDiscoveredEvent(
    Guid ProjectId,
    string AppId,
    string InstrumentationKey) : INotification;
