using C4.Modules.Discovery.Application.DiscoverResources;

namespace C4.Modules.Discovery.Application.Ports;

public interface IDiscoveryInputPlanner
{
    Task<DiscoveryPlan> BuildPlanAsync(string userIntent, string inputContext, CancellationToken cancellationToken);
}
