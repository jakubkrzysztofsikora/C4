namespace C4.Shared.Kernel;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}
