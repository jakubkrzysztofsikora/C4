using System.Text.Json;
using C4.Modules.Telemetry.Application.Ports;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

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
        var config = await configStore.GetAsync(projectId, cancellationToken);
        var appId = config?.AppId ?? configuration["ApplicationInsights:AppId"] ?? string.Empty;
        var apiKey = GlobalApiKey;

        if (string.IsNullOrWhiteSpace(appId) || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Application Insights not configured for project {ProjectId} (missing AppId or ApiKey); returning empty results", projectId);
            return [];
        }

        var kql = BuildHealthQuery(lookbackWindow);
        var encodedQuery = Uri.EscapeDataString(kql);
        var url = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={encodedQuery}";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        var response = await client.GetAsync(url, cancellationToken);
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Application Insights query failed ({StatusCode}): {Response}", response.StatusCode, responseJson);
            return [];
        }

        return ParseQueryResponse(responseJson);
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

    private static IReadOnlyCollection<ApplicationInsightsHealthRecord> ParseQueryResponse(string responseJson)
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

            var roleIndex = Array.IndexOf(columnNames, "cloud_RoleName");
            var scoreIndex = Array.IndexOf(columnNames, "score");
            var requestRateIndex = Array.IndexOf(columnNames, "requestRate");
            var errorRateIndex = Array.IndexOf(columnNames, "errorRate");
            var p95LatencyIndex = Array.IndexOf(columnNames, "p95LatencyMs");

            if (roleIndex < 0 || scoreIndex < 0)
                continue;

            foreach (var row in rows.EnumerateArray())
            {
                var cells = row.EnumerateArray().ToArray();
                var service = cells[roleIndex].GetString() ?? string.Empty;
                var score = TryReadDouble(cells[scoreIndex]) ?? 0;
                var requestRate = requestRateIndex >= 0 ? TryReadDouble(cells[requestRateIndex]) : null;
                var errorRate = errorRateIndex >= 0 ? TryReadDouble(cells[errorRateIndex]) : null;
                var p95Latency = p95LatencyIndex >= 0 ? TryReadDouble(cells[p95LatencyIndex]) : null;

                if (!string.IsNullOrWhiteSpace(service))
                    results.Add(new ApplicationInsightsHealthRecord(service, score, now, requestRate, errorRate, p95Latency));
            }
        }

        return results;
    }

    private static double? TryReadDouble(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Null || value.ValueKind == JsonValueKind.Undefined)
            return null;

        if (value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number))
            return number;

        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
            return parsed;

        return null;
    }
}
