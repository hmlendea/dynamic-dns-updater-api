# Dynamic DNS Updater API — Documentation Index

## Repository Summary

The Dynamic DNS Updater API is a .NET 10.0 web service that updates DNS A records for a given domain name to point to a specified IP address. It currently supports Gandi.net as a DNS provider and uses Quad9 DNS servers to verify whether a record already points to the requested IP address before making an update.

## Root Document Map

- [ARCHITECTURE.md](../ARCHITECTURE.md) — High-level architecture overview
- [SECURITY.md](../SECURITY.md) — Security model and threat mitigations
- [ROADMAP.md](../ROADMAP.md) — Project roadmap and planned features
- [PRIVACY.md](../PRIVACY.md) — Data handling and privacy policy
- [LICENSE](../LICENSE) — Licence terms

## Documentation Catalogue

### Getting Started
- [Quick Start](./quick-start.md) — Getting started guide for newcomers
- [Repository Overview](./repository-overview.md) — Repository purpose, scope, and entry points
- [Repository Structure](./repository-structure.md) — Source tree layout and module organisation

### Architecture & Design
- [Architecture](./architecture.md) — High-level architecture (complements root ARCHITECTURE.md)
- [Design Decisions](./design-decisions.md) — Key architectural and design choices with rationale
- [Components](./components/) — Per-component deep dives
  - [Presentation Layer](./components/presentation.md)
  - [Host and Composition](./components/host-and-composition.md)
  - [Application Services](./components/application-services.md)
  - [Integration Models](./components/integration-models.md)
- [Data Model](./data-model.md) — Domain entities, relationships, and persistence
- [Dependencies](./dependencies.md) — External and internal dependencies
- [Invariants](./invariants.md) — System-wide invariants and contracts

### API Reference
- [API Reference Index](./api-reference/INDEX.md) — Endpoint summary table and global policies
- [DnsRecords Controller](./api-reference/dnsrecords.md) — DNS records endpoint documentation

### Execution Flows
- [Startup and Request Pipeline](./flows/startup-and-rendering.md) — Application startup and middleware pipeline
- [DNS Record Update Flow](./flows/dns-record-update.md) — End-to-end DNS record update execution

### Behaviours
- [Update DNS Record](./behaviour/update-dns-record.md) — User-facing behaviour for updating DNS records

### Cross-Cutting Concerns
- [Configuration](./configuration.md) — Configuration schema, sources, and precedence
- [Error Handling](./error-handling.md) — Error taxonomy, handling patterns, and recovery
- [Logging](./logging.md) — Logging framework, levels, structured fields, correlation, sinks
- [Security](./security.md) — Security model, threats, and mitigations (complements root SECURITY.md)
- [Testing](./testing.md) — Test strategy, organisation, and coverage
- [Concurrency and Scheduling](./concurrency-and-scheduling.md) — Threading, async, schedulers, locks
- [State and Persistence](./state-and-persistence.md) — State management, stores, caches, migrations
- [Integrations](./integrations.md) — External system integrations (Gandi, Quad9)

### Operations
- [Build and Deployment](./build-and-deployment.md) — Build pipeline, deployment, environments
- [Troubleshooting](./troubleshooting.md) — Common issues and solutions
- [FAQ](./faq.md) — Frequently asked questions

### Maintenance
- [Change Guide](./change-guide.md) — How to modify common areas safely
- [Documentation Maintenance](./documentation-maintenance.md) — How to keep docs current, ownership
- [Ambiguities and Open Questions](./ambiguities-and-open-questions.md) — Unresolved items, TODOs, known gaps

### Examples
- [API Usage Examples](./api-usage-examples.md) — Example requests and responses for API usage

## Navigation Aids

- **Start here** (newcomers): [Quick Start](./quick-start.md) → [Repository Overview](./repository-overview.md) → [API Reference Index](./api-reference/INDEX.md)
- **Deep dive** (component owners): [Components](./components/) → [Architecture](./architecture.md) → [Design Decisions](./design-decisions.md)
- **Flows** (debuggers): [DNS Record Update Flow](./flows/dns-record-update.md) → [Startup and Request Pipeline](./flows/startup-and-rendering.md)

## Maintenance Metadata

- Last reviewed: 2026-10-15
- Owner: Dynamic DNS Updater team
- Coverage status: Initial generation — comprehensive coverage of current implementation