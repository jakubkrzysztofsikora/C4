namespace C4.Shared.Infrastructure.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
}
