using C4.Modules.Identity.Domain.Organization;
using C4.Shared.Kernel;

namespace C4.Modules.Identity.Domain.Events;

public sealed record OrganizationCreatedEvent(OrganizationId OrganizationId, string Name) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; } = DateTime.UtcNow;
}
