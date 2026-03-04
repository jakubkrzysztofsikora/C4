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
        var responsePayloads = await ExecuteKqlQueriesAsync(projectId, BuildHealthQuery(lookbackWindow), cancellationToken);
        if (responsePayloads.Count == 0) return [];

        var records = responsePayloads
            .SelectMany(ParseHealthQueryResponse)
            .ToArray();
        return AggregateHealthRecords(records);
    }

    public async Task<IReadOnlyCollection<ApplicationInsightsDependencyRecord>> QueryDependencyHealthAsync(
        Guid projectId,
        TimeSpan lookbackWindow,
        CancellationToken cancellationToken)
    {
        var responsePayloads = await ExecuteKqlQueriesAsync(projectId, BuildDependencyQuery(lookbackWindow), cancellationToken);
        if (responsePayloads.Count == 0) return [];

        var records = responsePayloads
            .SelectMany(ParseDependencyQueryResponse)
            .ToArray();
        return AggregateDependencyRecords(records);
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

    private async Task<IReadOnlyCollection<string>> ExecuteKqlQueriesAsync(
        Guid projectId,
        string kql,
        CancellationToken cancellationToken)
    {
        var config = await configStore.GetAsync(projectId, cancellationToken);
        var configuredAppIds = ParseAppIds(config?.AppId);
        if (configuredAppIds.Count == 0)
            configuredAppIds = ParseAppIds(configuration["ApplicationInsights:AppId"]);

        var apiKey = !string.IsNullOrWhiteSpace(config?.InstrumentationKey)
            ? config.InstrumentationKey.Trim()
            : GlobalApiKey;

        if (configuredAppIds.Count == 0 || string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning(
                "Application Insights not configured for project {ProjectId} (missing AppId(s) or API key); returning empty results",
                projectId);
            return [];
        }

        var encodedQuery = Uri.EscapeDataString(kql);
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);

        List<string> successfulPayloads = [];
        foreach (var appId in configuredAppIds)
        {
            var url = $"https://api.applicationinsights.io/v1/apps/{appId}/query?query={encodedQuery}";
            var response = await client.GetAsync(url, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Application Insights query failed for app {AppId} ({StatusCode}): {Response}",
                    appId,
                    response.StatusCode,
                    responseJson);
                continue;
            }

            successfulPayloads.Add(responseJson);
        }

        return successfulPayloads;
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

    private static List<string> ParseAppIds(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return [];

        return raw
            .Split([';', ',', '\n', '\r', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyCollection<ApplicationInsightsHealthRecord> AggregateHealthRecords(
        IReadOnlyCollection<ApplicationInsightsHealthRecord> records)
    {
        if (records.Count == 0)
            return [];

        return records
            .GroupBy(r => r.Service, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var totalRequestRate = group.Sum(record => record.RequestRate ?? 0);
                var weightedErrorNumerator = group.Sum(record => (record.ErrorRate ?? 0) * (record.RequestRate ?? 1));
                var weightedErrorDenominator = group.Sum(record => record.RequestRate ?? 1);
                var errorRate = weightedErrorDenominator > 0
                    ? weightedErrorNumerator / weightedErrorDenominator
                    : group.Average(record => record.ErrorRate ?? 0);
                var score = Math.Clamp(1 - errorRate, 0, 1);
                var p95Latency = group.Max(record => record.P95LatencyMs ?? 0);

                return new ApplicationInsightsHealthRecord(
                    group.Key,
                    Math.Round(score, 2),
                    group.Max(record => record.ObservedAtUtc),
                    totalRequestRate,
                    errorRate,
                    p95Latency);
            })
            .ToArray();
    }

    private static IReadOnlyCollection<ApplicationInsightsDependencyRecord> AggregateDependencyRecords(
        IReadOnlyCollection<ApplicationInsightsDependencyRecord> records)
    {
        if (records.Count == 0)
            return [];

        return records
            .GroupBy(record =>
                $"{NormalizeKey(record.SourceService)}->{NormalizeKey(record.TargetService)}|{NormalizeKey(record.Protocol)}",
                StringComparer.Ordinal)
            .Select(group =>
            {
                var requestRate = group.Sum(record => record.RequestRate);
                var weightedErrorNumerator = group.Sum(record => record.ErrorRate * Math.Max(record.RequestRate, 1));
                var weightedErrorDenominator = group.Sum(record => Math.Max(record.RequestRate, 1));
                var errorRate = weightedErrorDenominator > 0
                    ? weightedErrorNumerator / weightedErrorDenominator
                    : group.Average(record => record.ErrorRate);

                return new ApplicationInsightsDependencyRecord(
                    group.First().SourceService,
                    group.First().TargetService,
                    requestRate,
                    errorRate,
                    group.Max(record => record.P95LatencyMs),
                    group.Max(record => record.ObservedAtUtc),
                    group.First().Protocol);
            })
            .ToArray();
    }

    private static string NormalizeKey(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLowerInvariant();
}
