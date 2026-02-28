using C4.Shared.Kernel;

namespace C4.Modules.Discovery.Domain.McpServers;

public sealed class McpServerConfig : Entity<McpServerConfigId>
{
    private McpServerConfig(McpServerConfigId id, Guid projectId, string name, string endpoint, string authMode) : base(id)
    {
        ProjectId = projectId;
        Name = name;
        Endpoint = endpoint;
        AuthMode = authMode;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid ProjectId { get; }
    public string Name { get; private set; }
    public string Endpoint { get; private set; }
    public string AuthMode { get; private set; }
    public DateTime CreatedAtUtc { get; }

    public static McpServerConfig Create(Guid projectId, string name, string endpoint, string authMode = "None") =>
        new(McpServerConfigId.New(), projectId, name.Trim(), endpoint.Trim(), authMode);

    public void Update(string name, string endpoint, string authMode)
    {
        Name = name.Trim();
        Endpoint = endpoint.Trim();
        AuthMode = authMode;
    }
}
