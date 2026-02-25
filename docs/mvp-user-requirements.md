# High‑Level User‑Facing Requirements for a Live Architecture Diagram SaaS (MVP)

## Purpose

This document distils real user feedback from existing dependency‑mapping, network‑mapping and cloud‑visualisation tools (e.g., Faddom, Hyperglance, Cloudcraft, Auvik, Datadog and Kiali) into high‑level requirements for a minimum viable product.  These requirements reflect what users like and dislike in current solutions and what they expect from a platform that automatically visualises and monitors complex systems.  The MVP is scoped for **Microsoft Azure** environments and should integrate with **Bicep/Terraform** infrastructure‑as‑code (IaC) and **Azure Application Insights** for telemetry.

## Key Functional Requirements and User Expectations

### 1. Instant, agent‑less discovery of dependencies

* **Automatic dependency mapping:** Users praise tools that can instantly discover all servers, applications and network communications without manual configuration.  Reviews of Faddom highlight that it can provide *“clear, real‑time visualization of application dependencies and network topology without the need for deploying agents”*【607333491960853†L557-L559】 and that it reveals “hidden” connections between legacy applications【607333491960853†L420-L423】.  For an MVP, the platform must automatically map all resources and their interactions across Azure subscriptions, virtual networks and service boundaries.
* **Mapping unknown and legacy systems:** During cloud migrations, customers appreciate when tools highlight undocumented dependencies.  One user noted that Faddom showed how legacy applications were interconnected and helped plan migration waves to Azure【607333491960853†L695-L703】.  The MVP should discover orphaned resources and unknown communications so teams can plan refactoring and avoid breaking changes.

### 2. Real‑time, interactive visualisation with traffic overlays

* **Live diagrams that update automatically:** Users expect maps to stay in sync with the environment.  Hyperglance customers value its *“real‑time visibility into complex cloud environments”*【454409354574796†L1014-L1030】, while Faddom reviewers like that maps update quickly after setup【607333491960853†L618-L627】.  Cloudcraft users enjoy that it reads AWS infrastructure and creates diagrams automatically【62003380912234†L681-L686】.  The MVP must continuously poll Azure resources and refresh diagrams without manual refreshes.
* **Red/green traffic indicators:** Observability tools such as Datadog request flow maps and service maps colour nodes and edges based on health; the highest‑throughput edges are highlighted and service borders turn red or yellow when issues occur【525100085130389†L3889-L3914】【409339501965507†L3885-L3892】.  Sematext’s network map similarly colour‑codes connections so users *“see the connection turn red before your users complain”*【320412820081388†L101-L114】.  Users will expect an MVP to overlay real‑time Application Insights metrics—request rate, latency, errors—on each service and connection and use colours (green/yellow/red) to convey health.
* **Drill‑down and filtering:** Hyperglance users appreciate the ability to *“drill into details”* from high‑level topology to individual resources【454409354574796†L1014-L1030】.  However, some reviewers find the depth overwhelming at first【454409354574796†L1106-L1112】.  The MVP should allow zooming from a high‑level system view down to particular endpoints, filtering by resource type or tag and hiding noise to reduce cognitive overload.

### 3. Ease of setup and intuitive user experience

* **Rapid onboarding and minimal configuration:** Reviewers consistently laud tools that provide value within hours.  Faddom users were impressed that they could get maps *“in under 2 hours”*【607333491960853†L625-L627】 and that installation is easy【607333491960853†L565-L566】.  Cloudcraft’s drag‑and‑drop interface and automatic import of AWS resources make diagramming effortless【62003380912234†L746-L748】【62003380912234†L805-L808】.  Conversely, Hyperglance and Auvik users note a learning curve and initial configuration effort【454409354574796†L1106-L1112】【1523805047922†L618-L620】.  The MVP must offer a guided setup wizard, deploy via Azure Marketplace and avoid requiring agents wherever possible.
* **User‑friendly interface:** While many platforms are powerful, some reviews criticise cluttered interfaces or bland graphics.  Hyperglance users mention that the screen can feel *“confusing and cluttering”*【454409354574796†L956-L958】 and ask for a fancier graphical presentation【607333491960853†L841-L842】.  A clean, modern UI with intuitive controls and contextual help is essential for adoption.

### 4. Integration with Azure and IaC (Bicep/Terraform)

* **Azure‑native support:** Hyperglance customers value cross‑cloud visibility, noting that seeing AWS and Azure together on a single dashboard was *“invaluable”*【454409354574796†L1227-L1235】.  Faddom helped users plan migrations from on‑prem to Azure【607333491960853†L680-L703】.  The MVP must integrate with Azure Resource Graph, Azure Policy and App Insights to discover resources, apply tagging and fetch telemetry.  Multi‑subscription support and Azure Active Directory SSO should be included.
* **IaC synchronisation:** To keep documentation from going stale, the tool should parse Bicep/Terraform templates and compare desired state with the live environment.  Observability practitioners note that static documentation quickly becomes outdated, while trace‑driven maps reflect reality【424649603568102†L148-L154】.  The MVP should overlay the IaC plan onto the live map, highlight drift, and update diagrams when pull requests modifying infrastructure are merged.

### 5. Performance, scalability and reliability

* **Handle large environments without lag:** Hyperglance users report performance issues during large‑scale visualisations【454409354574796†L1370-L1374】 and note that screens can feel sluggish【454409354574796†L956-L958】.  A scalable backend that caches and streams topology updates will be critical.  The MVP should be validated on enterprise‑scale Azure deployments.
* **Resilient updates:** Automatic updates were occasionally problematic for Faddom【607333491960853†L629-L631】.  The MVP should provide safe upgrade mechanisms, version history and a rollback option to minimise downtime.

### 6. Health monitoring, cost insights and alerting

* **Real‑time health and performance metrics:** Users want to see not just topology, but also the performance of each component.  Datadog’s service map shows request counts and error rates【525100085130389†L3889-L3914】 and highlights unhealthy services with coloured borders【409339501965507†L3885-L3892】.  Kiali conveys service health by colouring lines green for healthy components and red/orange for issues【318662472783414†L156-L170】.  The MVP should integrate Azure Application Insights metrics, compute health scores, and animate traffic along paths.
* **Cost and resource optimisation:** Cloud professionals value tools that help manage costs.  Hyperglance users praise its cost monitoring and alerts that identify potential savings【454409354574796†L1361-L1366】, while some Auvik customers highlight the need for automation of remediation tasks【1523805047922†L538-L540】.  The MVP should surface unused or over‑provisioned resources and estimate cost impacts.
* **Customisable alerts:** Users expect configurable thresholds and notifications.  Some reviewers request more customisation options for reporting and alerts【454409354574796†L1034-L1037】【1523805047922†L689-L691】.  Integration with Azure Monitor, email, Teams and Slack should allow alerts on topology changes, performance degradations or IaC drift.

### 7. Collaboration, documentation and sharing

* **Export and embed diagrams:** Faddom is praised as a *“fantastic documentation tool”*【607333491960853†L430-L433】 and Cloudcraft provides isometric diagrams suitable for inclusion in Statements of Work【62003380912234†L741-L744】.  Auvik users like being able to export network maps for documentation【1523805047922†L690-L692】.  The MVP should allow exporting diagrams as images, PDF or markdown, embedding them in wikis and sharing interactive links with stakeholders.
* **Versioning and change history:** Users need to compare current and previous states.  Hyperglance provides historic views and automatic documentation, which users appreciate【454409354574796†L1292-L1294】.  The MVP should maintain a version history of diagrams, support diff views and comment threads for team collaboration.

### 8. Security and compliance insights

* **Detect misconfigurations and vulnerabilities:** Faddom helps confirm whether firewall changes were implemented【607333491960853†L430-L433】.  Hyperglance includes built‑in and custom security checks and alerts users to risks【454409354574796†L1226-L1228】.  The MVP should scan network security groups, Azure policies and identity configurations, surface open ports or policy violations and integrate with Microsoft Defender for Cloud.
* **Minimal privileges and data privacy:** Users value agent‑less approaches that do not require deep network access.  The MVP should adhere to least privilege principles and process telemetry in the customer’s own subscription or region where possible.

### 9. Pricing and licensing transparency

* **Clear and flexible pricing:** Some Hyperglance and Auvik users mention that licensing feels costly for new users【454409354574796†L1296-L1299】【1523805047922†L689-L691】 or that the licensing process requires manual renewal【607333491960853†L437-L438】.  To avoid frustration, the MVP should offer transparent, usage‑based pricing with simple tiering and self‑service subscription management via the Azure Marketplace.

### 10. Extensibility and future ecosystem

* **Plugin architecture and open API:** Users like the ability to integrate with email/stack platforms (Hyperglance can integrate with email and deliver alerts【454409354574796†L949-L952】).  The MVP should expose APIs and webhooks to integrate with DevOps pipelines, ticketing systems and code repositories.  Community‑developed plugins could extend support to additional clouds or telemetry sources.

## Summary

Real users of dependency‑mapping and observability tools emphasise **speed of discovery**, **clarity of visualisation**, **ease of use**, **real‑time health indicators**, **multi‑cloud support**, and **integration with existing workflows**.  Pain points include **complex, cluttered interfaces**, **performance lags in large environments**, **learning curves**, **limited customisation of alerts/reports**, and **opaque licensing**.  These insights should guide the MVP towards an Azure‑centric platform that automatically visualises architecture from IaC and live telemetry, overlays traffic and health metrics with intuitive red/green cues, supports cost and security insights, and enables collaboration and documentation—all while remaining simple to set up and reasonably priced.