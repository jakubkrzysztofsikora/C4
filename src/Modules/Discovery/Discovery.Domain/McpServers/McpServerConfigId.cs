using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.McpServers;

public sealed record McpServerConfigId(Guid Value) : StronglyTypedGuidId(Value)
{
    public static McpServerConfigId New() => new(Guid.NewGuid());
}
