using C4.Modules.Discovery.Domain.Resources;

namespace C4.Modules.Discovery.Application.Ports;

public interface IResourceClassifier
{
    Task<AzureResourceClassification> ClassifyAsync(Guid projectId, string armResourceType, string resourceName, CancellationToken cancellationToken);
}
