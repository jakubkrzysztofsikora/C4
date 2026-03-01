namespace C4.Modules.Discovery.Application.Ports;

public interface IOAuthStateStore
{
    Task StoreAsync(string state, CancellationToken cancellationToken);

    Task<bool> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken);
}
