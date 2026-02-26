using C4.Modules.Graph.Domain.Events;
using C4.Modules.Graph.Domain.GraphEdge;
using C4.Modules.Graph.Domain.GraphNode;
using C4.Modules.Graph.Domain.GraphSnapshot;
using C4.Shared.Kernel;

namespace C4.Modules.Graph.Domain.ArchitectureGraph;

public sealed class ArchitectureGraph : AggregateRoot<ArchitectureGraphId>
{
    private readonly List<C4.Modules.Graph.Domain.GraphNode.GraphNode> _nodes = [];
    private readonly List<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> _edges = [];
    private readonly List<C4.Modules.Graph.Domain.GraphSnapshot.GraphSnapshot> _snapshots = [];

    private ArchitectureGraph(ArchitectureGraphId id, Guid projectId) : base(id)
    {
        ProjectId = projectId;
    }

    public Guid ProjectId { get; }
    public IReadOnlyCollection<C4.Modules.Graph.Domain.GraphNode.GraphNode> Nodes => _nodes.AsReadOnly();
    public IReadOnlyCollection<C4.Modules.Graph.Domain.GraphEdge.GraphEdge> Edges => _edges.AsReadOnly();
    public IReadOnlyCollection<C4.Modules.Graph.Domain.GraphSnapshot.GraphSnapshot> Snapshots => _snapshots.AsReadOnly();

    public static ArchitectureGraph Create(Guid projectId) => new(ArchitectureGraphId.New(), projectId);

    public C4.Modules.Graph.Domain.GraphNode.GraphNode AddOrUpdateNode(string externalResourceId, string name, C4Level level)
    {
        var existing = _nodes.FirstOrDefault(n => n.ExternalResourceId == externalResourceId);
        var props = new NodeProperties("unknown", "n/a", [], 0m);
        if (existing is not null)
        {
            existing.Update(name, props);
            return existing;
        }

        var created = C4.Modules.Graph.Domain.GraphNode.GraphNode.Create(externalResourceId, name, level, null, props);
        _nodes.Add(created);
        return created;
    }

    public void ResolveNodeParents(IReadOnlyDictionary<string, string> parentExternalIdByChildExternalId)
    {
        foreach (var (childExternalId, parentExternalId) in parentExternalIdByChildExternalId)
        {
            var child = _nodes.FirstOrDefault(n => n.ExternalResourceId == childExternalId);
            var parent = _nodes.FirstOrDefault(n => n.ExternalResourceId == parentExternalId);
            if (child is not null && parent is not null)
                child.SetParent(parent.Id);
        }
    }

    public void AddEdge(C4.Modules.Graph.Domain.GraphNode.GraphNode source, C4.Modules.Graph.Domain.GraphNode.GraphNode target)
    {
        if (_edges.Any(e => e.SourceNodeId == source.Id && e.TargetNodeId == target.Id)) return;
        _edges.Add(C4.Modules.Graph.Domain.GraphEdge.GraphEdge.Create(source.Id, target.Id, new EdgeProperties("https", 443, "outbound")));
    }

    public C4.Modules.Graph.Domain.GraphSnapshot.GraphSnapshot CreateSnapshot()
    {
        var snapshot = C4.Modules.Graph.Domain.GraphSnapshot.GraphSnapshot.From(_nodes, _edges);
        _snapshots.Add(snapshot);
        Raise(new GraphUpdatedEvent(ProjectId, snapshot.Id));
        return snapshot;
    }
}
