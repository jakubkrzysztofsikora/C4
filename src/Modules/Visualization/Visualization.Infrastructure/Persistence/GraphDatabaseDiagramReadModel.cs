using System.Text.Json;
using C4.Modules.Visualization.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace C4.Modules.Visualization.Infrastructure.Persistence;

public sealed class GraphDatabaseDiagramReadModel(
    IConfiguration configuration,
    ILogger<GraphDatabaseDiagramReadModel> logger) : IDiagramReadModel
{
    public async Task<string?> GetDiagramJsonAsync(Guid projectId, CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("Graph");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            logger.LogWarning("Graph database connection string not configured; diagram unavailable");
            return null;
        }

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var graphId = await GetGraphIdAsync(connection, projectId, cancellationToken);
        if (graphId is null)
            return null;

        var nodes = await GetNodesAsync(connection, graphId.Value, cancellationToken);
        var edges = await GetEdgesAsync(connection, graphId.Value, cancellationToken);

        var diagram = new
        {
            projectId = projectId.ToString(),
            nodes,
            edges
        };

        return JsonSerializer.Serialize(diagram);
    }

    private static async Task<Guid?> GetGraphIdAsync(
        NpgsqlConnection connection,
        Guid projectId,
        CancellationToken cancellationToken)
    {
        await using var cmd = new NpgsqlCommand(
            """SELECT "Id" FROM architecture_graphs WHERE "ProjectId" = @projectId LIMIT 1""",
            connection);
        cmd.Parameters.AddWithValue("projectId", projectId);

        var result = await cmd.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : null;
    }

    private static async Task<List<object>> GetNodesAsync(
        NpgsqlConnection connection,
        Guid graphId,
        CancellationToken cancellationToken)
    {
        var nodes = new List<object>();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT "Id", "ExternalResourceId", "Name", "Level", "ParentId"
            FROM graph_nodes
            WHERE "ArchitectureGraphId" = @graphId
            """,
            connection);
        cmd.Parameters.AddWithValue("graphId", graphId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var levelLabel = reader.GetInt32(3) switch
            {
                0 => "Context",
                2 => "Component",
                _ => "Container"
            };

            var node = new Dictionary<string, object?>
            {
                ["id"] = reader.GetGuid(0).ToString(),
                ["externalResourceId"] = reader.GetString(1),
                ["name"] = reader.GetString(2),
                ["level"] = levelLabel
            };

            if (!reader.IsDBNull(4))
                node["parentNodeId"] = reader.GetGuid(4).ToString();

            nodes.Add(node);
        }

        return nodes;
    }

    private static async Task<List<object>> GetEdgesAsync(
        NpgsqlConnection connection,
        Guid graphId,
        CancellationToken cancellationToken)
    {
        var edges = new List<object>();

        await using var cmd = new NpgsqlCommand(
            """
            SELECT "Id", "SourceNodeId", "TargetNodeId"
            FROM graph_edges
            WHERE "ArchitectureGraphId" = @graphId
            """,
            connection);
        cmd.Parameters.AddWithValue("graphId", graphId);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            edges.Add(new Dictionary<string, object>
            {
                ["id"] = reader.GetGuid(0).ToString(),
                ["sourceNodeId"] = reader.GetGuid(1).ToString(),
                ["targetNodeId"] = reader.GetGuid(2).ToString()
            });
        }

        return edges;
    }
}
