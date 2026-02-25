namespace C4.Shared.Kernel;

public abstract record StronglyTypedId<TValue>(TValue Value) where TValue : notnull;

public abstract record StronglyTypedGuidId(Guid Value) : StronglyTypedId<Guid>(Value)
{
    public override string ToString() => Value.ToString();
}
