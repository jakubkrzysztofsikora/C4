using System.ComponentModel;
using Microsoft.SemanticKernel;

namespace C4.Modules.Discovery.Infrastructure.AI;

public sealed class DiscoveryPlannerToolsPlugin
{
    [KernelFunction("list_azure_connectors")]
    [Description("Lists Azure connector capabilities available for resource discovery.")]
    public string ListAzureConnectors()
        => "[{\"tool\":\"azure.resource_graph\",\"purpose\":\"Discover live Azure resources\",\"strength\":\"authoritative runtime inventory\"}]";

    [KernelFunction("list_repo_parsers")]
    [Description("Lists repository parser tools that infer architecture from IaC files.")]
    public string ListRepoParsers()
        => "[{\"tool\":\"repo.bicep_parser\",\"purpose\":\"Parse Bicep files\",\"strength\":\"design-time infrastructure intent\"},{\"tool\":\"repo.terraform_parser\",\"purpose\":\"Parse Terraform files\",\"strength\":\"multi-cloud IaC coverage\"}]";

    [KernelFunction("list_mcp_remote_tools")]
    [Description("Lists MCP remote tools that can enrich architecture context.")]
    public string ListMcpRemoteTools()
        => "[{\"tool\":\"mcp.remote_discovery\",\"purpose\":\"Query external tool registry\",\"strength\":\"cross-system enrichment\"}]";
}
