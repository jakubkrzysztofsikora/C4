namespace C4.Modules.Discovery.Application.Ports;

public interface IIacRepositoryStateProvider
{
    Task<IReadOnlyCollection<IacResourceRecord>> CollectAsync(
        Guid subscriptionId,
        string? environment,
        CancellationToken cancellationToken);
}
