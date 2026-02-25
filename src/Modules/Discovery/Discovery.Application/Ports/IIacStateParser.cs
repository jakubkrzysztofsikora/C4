namespace C4.Modules.Discovery.Application.Ports;

public interface IIacStateParser
{
    Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken);
}

public sealed record IacResourceRecord(string ResourceId, string ResourceType, string Name);
