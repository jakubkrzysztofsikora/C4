# Progress: Discovery Source Integration (App Insights + Git IaC + MCP)
Scope: Feature
Created: 2026-02-27
Last Updated: 2026-02-27
Status: In Progress

## Overview

Three discovery source integrations that are currently stubs, plus App Insights auto-configuration:

1. **App Insights** - Auto-discover AppId/ApiKey from Azure subscription resources (no separate config)
2. **Git IaC** - User configures Git repo URL + credentials in subscription UI; adapter clones and parses Bicep/Terraform
3. **MCP Servers** - User configures MCP server endpoints from app UI (like Claude.ai)

## Task Progress

### Epic 1: App Insights Auto-Discovery
- [x] 1.1 – Query Azure Resource Graph for Microsoft.Insights/components resources
- [x] 1.2 – Extract AppId and generate API key from discovered App Insights resources
- [x] 1.3 – Pass App Insights config to Telemetry module for health queries

### Epic 2: Git Repository IaC Integration
- [x] 2.1 – Add GitRepoUrl + GitPat fields to AzureSubscription domain entity
- [x] 2.2 – Update ConnectSubscription endpoint/handler to accept repo config
- [x] 2.3 – Create EF migration for new columns
- [x] 2.4 – Implement RepositoryIacDiscoverySourceAdapter (clone + parse)
- [x] 2.5 – Add Git repo config fields to SubscriptionWizardPage UI
- [x] 2.6 – Wire up frontend API calls with new fields

### Epic 3: MCP Server Configuration
- [x] 3.1 – Create McpServerConfig domain entity + repository
- [x] 3.2 – Create CRUD endpoints for MCP server configs
- [x] 3.3 – Implement RemoteMcpDiscoverySourceAdapter (call MCP tools)
- [x] 3.4 – Add MCP config UI to SubscriptionWizardPage
- [x] 3.5 – Wire up frontend CRUD calls

### Epic 4: Build & Deploy
- [ ] 4.1 – Build and run all tests
- [ ] 4.2 – Commit and push

## Decisions Log
| Date | Decision | Context |
|------|----------|---------|
| 2026-02-27 | App Insights discovered from Azure Resource Graph, not separate config | User requirement |
| 2026-02-27 | Git repo URL + PAT configured alongside subscription in UI | User requirement |
| 2026-02-27 | MCP servers configurable from app UI like Claude.ai | User requirement |
