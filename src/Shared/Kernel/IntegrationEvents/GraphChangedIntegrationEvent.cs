using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record GraphChangedIntegrationEvent(Guid ProjectId, string Trigger, DateTime ChangedAtUtc) : INotification;
