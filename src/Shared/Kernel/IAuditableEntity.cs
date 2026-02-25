namespace C4.Shared.Kernel;

public interface IAuditableEntity
{
    DateTime CreatedUtc { get; }
    DateTime? ModifiedUtc { get; }
}
