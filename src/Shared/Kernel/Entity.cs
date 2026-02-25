namespace C4.Shared.Kernel;

public abstract class Entity<TId>(TId id) where TId : notnull
{
    public TId Id { get; } = id;
}
