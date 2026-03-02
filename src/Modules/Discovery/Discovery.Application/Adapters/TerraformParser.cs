using C4.Modules.Discovery.Application.Ports;
using System.Text.RegularExpressions;

namespace C4.Modules.Discovery.Application.Adapters;

public sealed class TerraformParser : IIacStateParser
{
    private static readonly Regex ResourceDeclarationRegex = new(
        "^resource\\s+\"(?<type>[^\"]+)\"\\s+\"(?<name>[^\"]+)\"",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public Task<IReadOnlyCollection<IacResourceRecord>> ParseAsync(string iacContent, string format, CancellationToken cancellationToken)
    {
        if (!string.Equals(format, "terraform", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(format, "tf", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>([]);
        }

        var lines = iacContent.Split('\n');
        List<IacResourceRecord> resources = [];
        foreach (var raw in lines)
        {
            var line = raw.Trim();
            var match = ResourceDeclarationRegex.Match(line);
            if (!match.Success)
                continue;

            var terraformType = match.Groups["type"].Value;
            var logicalName = match.Groups["name"].Value;
            var mappedArmType = MapTerraformType(terraformType);
            var resourceId = $"/providers/{mappedArmType}/{logicalName}".ToLowerInvariant();

            resources.Add(new IacResourceRecord(resourceId, mappedArmType, line));
        }

        return Task.FromResult<IReadOnlyCollection<IacResourceRecord>>(resources);
    }

    private static string MapTerraformType(string terraformType)
    {
        var normalized = terraformType.Trim().ToLowerInvariant();
        return normalized switch
        {
            "azurerm_storage_account" => "microsoft.storage/storageaccounts",
            "azurerm_app_service" => "microsoft.web/sites",
            "azurerm_linux_web_app" => "microsoft.web/sites",
            "azurerm_windows_web_app" => "microsoft.web/sites",
            "azurerm_function_app" => "microsoft.web/sites",
            "azurerm_linux_function_app" => "microsoft.web/sites",
            "azurerm_windows_function_app" => "microsoft.web/sites",
            "azurerm_mssql_server" => "microsoft.sql/servers",
            "azurerm_mssql_database" => "microsoft.sql/servers/databases",
            "azurerm_postgresql_flexible_server" => "microsoft.dbforpostgresql/flexibleservers",
            "azurerm_redis_cache" => "microsoft.cache/redis",
            "azurerm_servicebus_namespace" => "microsoft.servicebus/namespaces",
            "azurerm_servicebus_queue" => "microsoft.servicebus/namespaces/queues",
            "azurerm_virtual_network" => "microsoft.network/virtualnetworks",
            "azurerm_subnet" => "microsoft.network/virtualnetworks/subnets",
            "azurerm_network_interface" => "microsoft.network/networkinterfaces",
            "azurerm_application_insights" => "microsoft.insights/components",
            _ => normalized.Replace("_", "/", StringComparison.Ordinal)
        };
    }
}
