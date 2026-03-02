using System.Text.Json;
using C4.Modules.Telemetry.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace C4.Modules.Telemetry.Infrastructure.Adapters;

public sealed class ApplicationInsightsClient(
    IHttpClientFactory httpClientFactory,
    IAppInsightsConfigStore configStore,
    IConfiguration configuration,
    ILogger<ApplicationInsightsClient> logger) : IApplicationInsightsClient
{
    private string GlobalApiKey => configuration["ApplicationInsights:ApiKey"] ?? string.Empty;

    public async Task<IReadOnlyCollection<ApplicationInsightsHealthRecord>> QueryServiceHealthAsync(
        Guid projectId,
        TimeSpan lookbackWindow,
        CancellationToken cancellationToken)
    {
        var responseJson = await ExecuteKqlQueryAsync(projectId, BuildHealthQuery(lookbackWindow), cancellationToken);
        if (responseJson is null) return [];

        return ParseHealthQueryResponse(responseJson);
    }

    public async Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(
        Guid projectId,
        TimeSpan lookbackWindow,
        CancellationToken cancellationToken)
    {
        var responseJson = await ExecuteKqlQueryAsync(projectId, BuildDependencyQuery(lookbackWindow), cancellationToken);
        if (responseJson is null) return [];

        return ParseDependencyQueryResponse(responseJson);
    }

    private static string BuildHealthQuery(TimeSpan lookbackWindow)
    {
        var minutes = (int)lookbackWindow.TotalMinutes;
        return $"""
            requests
            | where timestamp > ago({minutes}m)
            | summarize
                totalRequests = count(),
                failedRequests = countif(success == false),
                p95LatencyMs = percentile(duration / 1ms, 95)
              by cloud_RoleName
            | where totalRequests > 0
            | extend requestRate = todouble(totalRequests) / todouble({minutes})
            | extend errorRate = todouble(failedRequests) / todouble(totalRequests)
            | extend score = round(1.0 - errorRate, 2)
            | project cloud_RoleName, score, requestRate, errorRate, p95LatencyMs
            """;
    }

    private static string BuildDependencyQuery(TimeSpan lookbackWindow)
    {
        var minutes = (int)lookbackWindow.TotalMinutes;
        return $"""
            dependencies
            | where timestamp > ago({minutes}m)
            | summarize
                totalCalls = count(),
                failedCalls = countif(success == false),
                p95LatencyMs = percentile(duration / 1ms, 95)
              by cloud_RoleName, target, type
            | where totalCalls > 0
            | extend requestRate = todouble(totalCalls) / todouble({minutes})
            | extend errorRate = todouble(failedCalls) / todouble(totalCalls)
            | project cloud_RoleName, target, type, requestRate, errorRate, p95LatencyMs
            """;
    }

    private async Task<string?> ExecuteKqlQueryAsync(
        Guid projectId,
        string kql,
        CancellationToken cancellationToken)
    {
        var config = await configStore.GetAsync(projectId, cancellationToken);
        var appId = config?.AppId ?? configuration["ApplicationInsights:AppId"] ?? string.Empty;
        var apiKey = GlobalApiKey;

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "Application Insights not configured for project {ProjectId} (missing AppId or ApiKey); returning empty results",
                projectId);
            return null;
        }

        var encodedQuery = Uri.EscapeDataString(kql);
        var url = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={encodedQuery}";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var response = await client.GetAsync(url, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Application Insights query failed ({StatusCode}): {Response}", response.StatusCode, responseJson);
            return null;
        }

        return responseJson;
    }

    private static IReadOnlyCollection<ApplicationInsightsHealthRecord> ParseHealthQueryResponse(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("tables", out var tables))
            return [];

        var results = new List<ApplicationInsightsHealthRecord>();
        var now = DateTime.UtcNow;

        foreach (var table in tables.EnumerateArray())
        {
            if (!table.TryGetProperty("columns", out var columns) || !table.TryGetProperty("rows", out var rows))
                continue;

            var columnNames = columns.EnumerateArray()
                .Select(c => c.GetProperty("name").GetString() ?? string.Empty)
                .ToArray();

            var roleIndex = FindColumnIndex(columnNames, "cloud_RoleName");
            var scoreIndex = FindColumnIndex(columnNames, "score");
            var requestRateIndex = FindColumnIndex(columnNames, "requestRate");
            var errorRateIndex = FindColumnIndex(columnNames, "errorRate");
            var p95LatencyIndex = FindColumnIndex(columnNames, "p95LatencyMs");

            if (roleIndex < 0 || scoreIndex < 0)
                continue;

            foreach (var row in rows.EnumerateArray())
            {
                var cells = row.EnumerateArray().ToArray();
                var service = TryReadString(cells, roleIndex) ?? string.Empty;
                var score = TryReadDouble(cells, scoreIndex) ?? 0;
                var requestRate = TryReadDouble(cells, requestRateIndex);
                var errorRate = TryReadDouble(cells, errorRateIndex);
                var p95Latency = TryReadDouble(cells, p95LatencyIndex);

                if (!string.IsNullOrWhiteSpace(service))
                    results.Add(new ApplicationInsightsHealthRecord(service, score, now, requestRate, errorRate, p95Latency));
            }
        }

        return results;
    }

    private static IReadOnlyCollection<ApplicationInsightsDependencyRecord> ParseDependencyQueryResponse(string responseJson)
    {
        using var document = JsonDocument.Parse(responseJson);
        var root = document.RootElement;

        if (!root.TryGetProperty("tables", out var tables))
            return [];

        var results = new List<ApplicationInsightsDependencyRecord>();
        var now = DateTime.UtcNow;

        foreach (var table in tables.EnumerateArray())
        {
            if (!table.TryGetProperty("columns", out var columns) || !table.TryGetProperty("rows", out var rows))
                continue;

            var columnNames = columns.EnumerateArray()
                .Select(c => c.GetProperty("name").GetString() ?? string.Empty)
                .ToArray();

            var roleIndex = FindColumnIndex(columnNames, "cloud_RoleName");
            var targetIndex = FindColumnIndex(columnNames, "target");
            var typeIndex = FindColumnIndex(columnNames, "type");
            var requestRateIndex = FindColumnIndex(columnNames, "requestRate");
            var errorRateIndex = FindColumnIndex(columnNames, "errorRate");
            var p95LatencyIndex = FindColumnIndex(columnNames, "p95LatencyMs");

            if (roleIndex < 0 || targetIndex < 0 || requestRateIndex < 0 || errorRateIndex < 0 || p95LatencyIndex < 0)
                continue;

            foreach (var row in rows.EnumerateArray())
            {
                var cells = row.EnumerateArray().ToArray();
                var sourceService = TryReadString(cells, roleIndex) ?? string.Empty;
                var targetService = TryReadString(cells, targetIndex) ?? string.Empty;
                var protocol = TryReadString(cells, typeIndex);

                var requestRate = TryReadDouble(cells, requestRateIndex) ?? 0;
                var errorRate = TryReadDouble(cells, errorRateIndex) ?? 0;
                var p95LatencyMs = TryReadDouble(cells, p95LatencyIndex) ?? 0;

                if (string.IsNullOrWhiteSpace(sourceService) || string.IsNullOrWhiteSpace(targetService))
                    continue;

                results.Add(new ApplicationInsightsDependencyRecord(
                    sourceService,
                    targetService,
                    requestRate,
                    errorRate,
                    p95LatencyMs,
                    now,
                    protocol));
            }
        }

        return results;
    }

    private static int FindColumnIndex(string[] columnNames, string expectedName)
        => Array.FindIndex(columnNames, name => name.Equals(expectedName, StringComparison.OrdinalIgnoreCase));

    private static string? TryReadString(JsonElement[] cells, int index)
    {
        if (index < 0 || index >= cells.Length) return null;
        var value = cells[index];
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined) return null;
        if (value.ValueKind == JsonValueKind.String) return value.GetString();
        return value.ToString();
    }

    private static double? TryReadDouble(JsonElement[] cells, int index)
    {
        if (index < 0 || index >= cells.Length) return null;
        return TryReadDouble(cells[index]);
    }

    private static double? TryReadDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return double.IsFinite(number) ? number : null;

        if (value.ValueKind == JsonValueKind.String
            && double.TryParse(value.GetString(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
            return double.IsFinite(parsed) ? parsed : null;

        return null;
    }
}
