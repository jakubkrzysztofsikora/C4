# Circit Baseline Architecture Context

## Project Description
Circit runs a multi-service Azure ecosystem centered on `circit-{env}` core workloads that power AP automation, document ingestion, financial workflow processing, and integration with external banking and SaaS systems. The platform spans production and non-production environments, with a dense mix of App Services, Function Apps, Container Apps, data stores, messaging, private networking, and observability resources.

## System Boundaries
- Internet and external users interact with public application entry points.
- Core runtime boundary is the Azure subscription and its environment-scoped resource groups.
- Internal trust boundaries separate internet-facing services, application services, data services, and private networking.

## Core Domains
- `CoreApp`: primary customer-facing application workloads and APIs.
- `DocumentService`: document ingestion, processing, and storage paths.
- `OpenBanking`: banking data ingestion and synchronization services.
- `Platform`: shared infrastructure, observability, and internal enablement services.

## External Dependencies
- Azure AD / identity providers.
- Banking and open-banking external APIs.
- Notification and communication providers (for example, email and chat integrations).
- AI/LLM external services used by selected product capabilities.

## Data Sensitivity
- Financial documents and transaction metadata.
- User and account-level PII.
- Security credentials and integration secrets.

## Clarifying Questions For Architecture Completion
1. Which exact services constitute the `circit-{env}` core runtime path in production (frontend, API, workers, workflows)?
2. Which domains (`CoreApp`, `DocumentService`, `OpenBanking`, `Platform`) own each top-level resource group?
3. Which critical data flows are synchronous versus asynchronous between core services?
4. Which external systems are mandatory dependencies for production transaction success?
5. Which data stores contain regulated data, and what retention/encryption controls are required per store?
6. Which trust boundaries must be explicit in diagrams (internet edge, identity boundary, data boundary, cross-region boundary)?
7. Which environments should be included in default diagrams (`production` only vs `production + core non-prod`)?
8. Which resources are infrastructure-only and should be hidden by default outside component view?
9. Which failure domains are unacceptable for downtime (for example, OpenBanking sync, document ingestion, auth path)?
10. Which threat scenarios must be prioritized first (credential abuse, data exfiltration, queue poisoning, lateral movement)?

