using MediatR;

namespace C4.Shared.Kernel.IntegrationEvents;

public sealed record FeedbackSubmittedIntegrationEvent(Guid ProjectId, Guid FeedbackEntryId, string Category, int Rating) : INotification;
